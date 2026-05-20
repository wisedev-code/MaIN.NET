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

public sealed class AnthropicSkillUploader : IProviderSkillUploader
{
    private readonly MaINSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public AnthropicSkillUploader(
        MaINSettings settings,
        IHttpClientFactory httpClientFactory,
        ILogger<AnthropicSkillUploader>? logger = null)
    {
        _settings = settings;
        _httpClientFactory = httpClientFactory;
        _logger = logger ?? NullLogger<AnthropicSkillUploader>.Instance;
    }

    public BackendType Backend => BackendType.Anthropic;

    public bool HasCredentials() =>
        !string.IsNullOrEmpty(_settings.AnthropicKey) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(LLMApiRegistry.Anthropic.ApiKeyEnvName));

    // Anthropic Skills versioning isn't publicly documented; the existingSkillId parameter on the
    // interface is ignored here. Re-upload always creates a brand new skill_id — the coordinator
    // detects the id change and calls DeleteAsync to clean up the orphan.
    public async Task<ProviderSkillReference> UploadAsync(AgentSkill skill, string? existingSkillId = null, CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrEmpty(skill.BundlePath))
            throw new InvalidOperationException($"Skill '{skill.Name}' has no bundle path to upload.");

        var apiKey = _settings.AnthropicKey ?? Environment.GetEnvironmentVariable(LLMApiRegistry.Anthropic.ApiKeyEnvName)
                     ?? throw new APIKeyNotConfiguredException(LLMApiRegistry.Anthropic.ApiName);

        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.AnthropicClient);

        var zipPath = ResolveZipPath(skill);
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(skill.Name), "display_title");

            // StreamContent owns the FileStream — it's disposed alongside the multipart content.
            var fileContent = new StreamContent(File.OpenRead(zipPath));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "skill", $"{skill.Name}.zip");

            using var request = new HttpRequestMessage(HttpMethod.Post, ServiceConstants.ApiUrls.AnthropicSkills)
            {
                Content = content
            };
            // Per-request headers — see OpenAiSkillUploader for the rationale.
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Headers.Add("anthropic-beta", ServiceConstants.AnthropicBetaFeatures.SkillsBetaHeader);

            using var response = await client.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Anthropic skill upload for '{skill.Name}' failed ({(int)response.StatusCode}): {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var skillId = root.TryGetProperty("skill_id", out var sidEl)
                ? sidEl.GetString()
                : root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;

            if (string.IsNullOrEmpty(skillId))
                throw new InvalidOperationException($"Anthropic skill upload response missing skill_id: {json}");

            var version = root.TryGetProperty("version", out var verEl) ? verEl.ToString() : null;

            _logger.LogInformation("Uploaded skill '{Skill}' to Anthropic as {SkillId} (version {Version}).",
                skill.Name, skillId, version ?? "latest");

            return new ProviderSkillReference
            {
                Name = skill.Name,
                SkillId = skillId,
                Version = version,
                Backend = BackendType.Anthropic
            };
        }
        finally
        {
            SkillBundleZipper.TryDelete(zipPath);
        }
    }

    public async Task DeleteAsync(string skillId, CancellationToken cancellationToken = default)
    {
        var apiKey = _settings.AnthropicKey ?? Environment.GetEnvironmentVariable(LLMApiRegistry.Anthropic.ApiKeyEnvName);
        if (string.IsNullOrEmpty(apiKey)) return;

        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.AnthropicClient);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceConstants.ApiUrls.AnthropicSkills}/{skillId}");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Add("anthropic-beta", ServiceConstants.AnthropicBetaFeatures.SkillsBetaHeader);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Anthropic skill delete for {SkillId} returned {Status}: {Body}",
                    skillId, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic skill delete for {SkillId} threw.", skillId);
        }
    }

    public async Task<IReadOnlyDictionary<string, ProviderSkillReference>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ProviderSkillReference>(StringComparer.OrdinalIgnoreCase);

        var apiKey = _settings.AnthropicKey ?? Environment.GetEnvironmentVariable(LLMApiRegistry.Anthropic.ApiKeyEnvName);
        if (string.IsNullOrEmpty(apiKey)) return result;

        var client = _httpClientFactory.CreateClient(ServiceConstants.HttpClients.AnthropicClient);
        // ?source=custom excludes Anthropic-maintained pre-built skills (pptx, xlsx, docx, pdf)
        // so reconciliation only matches user-uploaded skills against the local cache.
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ServiceConstants.ApiUrls.AnthropicSkills}?source=custom");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Add("anthropic-beta", ServiceConstants.AnthropicBetaFeatures.SkillsBetaHeader);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anthropic skill list returned {Status}: {Body}", (int)response.StatusCode, json);
                return result;
            }

            using var doc = JsonDocument.Parse(json);
            var arr = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement
                : doc.RootElement.TryGetProperty("data", out var dataEl) ? dataEl : default;

            if (arr.ValueKind != JsonValueKind.Array) return result;

            foreach (var item in arr.EnumerateArray())
            {
                var id = item.TryGetProperty("skill_id", out var sidEl) ? sidEl.GetString()
                       : item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var name = item.TryGetProperty("display_title", out var titleEl) ? titleEl.GetString()
                         : item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                var version = item.TryGetProperty("version", out var verEl) ? verEl.ToString() : null;
                if (id is null || name is null) continue;

                result[name] = new ProviderSkillReference
                {
                    Name = name,
                    SkillId = id,
                    Version = version,
                    Backend = BackendType.Anthropic
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Anthropic skill list threw.");
        }

        return result;
    }

    private static string ResolveZipPath(AgentSkill skill)
    {
        var bundlePath = skill.BundlePath!;

        if (Directory.Exists(bundlePath))
            return SkillBundleZipper.ZipDirectory(bundlePath);

        if (File.Exists(bundlePath))
            return SkillBundleZipper.ZipSingleFile(skill.Name, bundlePath);

        throw new InvalidOperationException($"Skill '{skill.Name}' bundle path '{bundlePath}' does not exist.");
    }
}
