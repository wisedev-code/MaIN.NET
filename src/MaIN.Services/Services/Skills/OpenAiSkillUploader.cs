using System.Net.Http.Headers;
using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;
using MaIN.Domain.Exceptions;
using MaIN.Domain.Models.Concrete;
using MaIN.Services.Constants;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MaIN.Services.Services.Skills;

public sealed class OpenAiSkillUploader : IProviderSkillUploader
{
    private readonly MaINSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public OpenAiSkillUploader(
        MaINSettings settings,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAiSkillUploader>? logger = null)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
        _logger = logger ?? NullLogger<OpenAiSkillUploader>.Instance;
    }

    public BackendType Backend => BackendType.OpenAi;

    public bool HasCredentials() =>
        !string.IsNullOrEmpty(_settings.OpenAiKey) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LLMApiRegistry.OpenAi.ApiKeyEnvName));

    public async Task<ProviderSkillReference> UploadAsync(AgentSkill skill, string? existingSkillId = null, CancellationToken cancellationToken = default)
    {
        // Fail fast before any HttpClient allocation or auth resolution.
        if (string.IsNullOrEmpty(skill.BundlePath))
            throw new InvalidOperationException($"Skill '{skill.Name}' has no bundle path to upload.");

        var isDirectory = Directory.Exists(skill.BundlePath);
        var isFile = !isDirectory && File.Exists(skill.BundlePath);
        if (!isDirectory && !isFile)
            throw new InvalidOperationException($"Skill '{skill.Name}' bundle path '{skill.BundlePath}' does not exist.");

        var apiKey = _settings.OpenAiKey ?? Environment.GetEnvironmentVariable(LLMApiRegistry.OpenAi.ApiKeyEnvName)
                     ?? throw new APIKeyNotConfiguredException(LLMApiRegistry.OpenAi.ApiName);

        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.OpenAiClient);

        // When we already have a skill_id, push a new VERSION under it
        // (POST /v1/skills/{id}/versions) instead of creating a duplicate skill. Then point
        // default_version at the new version so skill_reference without explicit `version` and
        // with `"latest"` both resolve to the freshest bundle.
        var uploadUrl = string.IsNullOrEmpty(existingSkillId)
            ? ServiceConstants.ApiUrls.OpenAiSkills
            : $"{ServiceConstants.ApiUrls.OpenAiSkills}/{existingSkillId}/versions";

        // Directory → zip mode per docs: "Zip the top-level folder and upload the zip file".
        // Lone .md → per-file mode (no folder to wrap).
        var reference = isDirectory
            ? await UploadAsZipAsync(skill, client, apiKey, uploadUrl, cancellationToken)
            : await UploadAsSingleFileAsync(skill, client, apiKey, uploadUrl, cancellationToken);

        // Versioned upload returns a new version number under the same skill_id; promote it to
        // default so callers that omit `version` or pass `"latest"` get the bump immediately.
        if (!string.IsNullOrEmpty(existingSkillId) && !string.IsNullOrEmpty(reference.Version) &&
            int.TryParse(reference.Version, out var versionInt))
        {
            await SetDefaultVersionAsync(client, apiKey, existingSkillId, versionInt, cancellationToken);
            // Preserve the original skill_id — the versioned endpoint may have returned the version
            // resource id rather than the parent skill id depending on shape.
            reference = new ProviderSkillReference
            {
                Name = skill.Name,
                SkillId = existingSkillId,
                Version = reference.Version,
                Backend = BackendType.OpenAi
            };
        }

        return reference;
    }

    private async Task SetDefaultVersionAsync(HttpClient client, string apiKey, string skillId, int versionInt, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ServiceConstants.ApiUrls.OpenAiSkills}/{skillId}")
        {
            Content = new StringContent(
                $"{{\"{ServiceConstants.OpenAiResponses.DefaultVersionField}\":{versionInt}}}",
                System.Text.Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OpenAI set default_version={Version} for {SkillId} returned {Status}: {Body}",
                    versionInt, skillId, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI set default_version={Version} for {SkillId} threw.", versionInt, skillId);
        }
    }

    private async Task<ProviderSkillReference> UploadAsZipAsync(AgentSkill skill, HttpClient client, string apiKey, string url, CancellationToken cancellationToken)
    {
        // Per docs: "Zip the top-level folder and upload the zip file" — zip layout becomes
        // "{skill}/SKILL.md" + sibling files under the same prefix.
        var zipPath = SkillBundleZipper.ZipDirectory(skill.BundlePath!);
        try
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(File.OpenRead(zipPath));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "files", $"{skill.Name}.zip");

            return await SendAndParseAsync(skill, client, content, apiKey, url, cancellationToken);
        }
        finally
        {
            SkillBundleZipper.TryDelete(zipPath);
        }
    }

    private static string? ExtractVersionString(JsonElement root, string fieldName)
    {
        if (!root.TryGetProperty(fieldName, out var el)) return null;
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Null => null,
            _ => el.ToString()
        };
    }

    private async Task<ProviderSkillReference> UploadAsSingleFileAsync(AgentSkill skill, HttpClient client, string apiKey, string url, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(File.OpenRead(skill.BundlePath!));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");

        // Mirror the curl example:
        //   files[]=@./basic_math/SKILL.md;filename=basic_math/SKILL.md;type=text/markdown
        // The skill name becomes the namespace prefix so the server's manifest discovery
        // treats it as that skill's SKILL.md.
        var transportName = $"{skill.Name}/SKILL.md";
        content.Add(fileContent, "files[]", transportName);

        return await SendAndParseAsync(skill, client, content, apiKey, url, cancellationToken);
    }

    // OpenAI Skills API does not expose DELETE or LIST endpoints as of the public docs at
    // https://developers.openai.com/api/docs/guides/tools-skills.
    // Cleanup of stale skill_ids relies on versioning instead — POST /v1/skills/{id}/versions
    // creates a new version under the same skill_id, so re-uploads never produce orphans.
    //
    // Listing is unavailable, so reconcile-from-provider can't repopulate the cache after a wipe
    // for OpenAI; the first re-upload after a wipe will create a duplicate skill on the account.
    // Mitigation: persist the cache file (volume mount in containers / committed in CI).

    public Task DeleteAsync(string skillId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("OpenAI Skills API has no DELETE endpoint; ignoring delete request for {SkillId}.", skillId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, ProviderSkillReference>> ListAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("OpenAI Skills API has no LIST endpoint; reconcile-from-provider is a no-op for OpenAI.");
        return Task.FromResult<IReadOnlyDictionary<string, ProviderSkillReference>>(
            new Dictionary<string, ProviderSkillReference>(StringComparer.OrdinalIgnoreCase));
    }

    private async Task<ProviderSkillReference> SendAndParseAsync(
        AgentSkill skill,
        HttpClient client,
        MultipartFormDataContent content,
        string apiKey,
        string url,
        CancellationToken cancellationToken)
    {
        // Auth header set per-request rather than on DefaultRequestHeaders — the HttpClient
        // returned by IHttpClientFactory may share its handler chain across callers, and
        // mutating default headers there can leak credentials or trigger duplicate-header throws.
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"OpenAI skill upload for '{skill.Name}' failed ({(int)response.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Prefer the explicit skill_id field; fall back to id only when skill_id is absent. The
        // versioned endpoint POST /v1/skills/{id}/versions can return BOTH `id` (= version id) and
        // `skill_id` (= parent) — using `id` first there would cache a version_id and the next
        // Responses call would 404 because the skill it references doesn't exist by that name.
        var skillId = root.TryGetProperty("skill_id", out var sidEl) ? sidEl.GetString()
                    : root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

        if (string.IsNullOrEmpty(skillId))
            throw new InvalidOperationException($"OpenAI skill upload response missing id: {json}");

        // OpenAI Skills response shape (observed): { id, default_version, latest_version, ... }.
        // Versioned endpoint may also return { version }. Probe all three; pinned ints win over
        // "latest" alias so subsequent skill_reference calls have a concrete version to resolve.
        string? version = ExtractVersionString(root, "version")
                       ?? ExtractVersionString(root, "latest_version")
                       ?? ExtractVersionString(root, "default_version");

        _logger.LogInformation("Uploaded skill '{Skill}' to OpenAI as {SkillId} (version {Version}).",
            skill.Name, skillId, version ?? "(unspecified)");

        return new ProviderSkillReference
        {
            Name = skill.Name,
            SkillId = skillId,
            Version = version,
            Backend = BackendType.OpenAi
        };
    }

}
