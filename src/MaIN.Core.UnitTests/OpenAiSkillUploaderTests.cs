using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities.Skills;
using MaIN.Services.Services.Skills;

namespace MaIN.Core.UnitTests;

public class OpenAiSkillUploaderTests : IDisposable
{
    private readonly string _bundleDir;

    public OpenAiSkillUploaderTests()
    {
        _bundleDir = Path.Combine(Path.GetTempPath(), $"main-uploader-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_bundleDir);
        File.WriteAllText(Path.Combine(_bundleDir, "SKILL.md"), """
            ---
            name: code-review
            description: Review code
            ---
            body
            """);
    }

    public void Dispose()
    {
        try { Directory.Delete(_bundleDir, recursive: true); } catch { /* best-effort */ }
    }

    private static AgentSkill MakeSkill(string bundlePath) => new()
    {
        Name = "code-review",
        Description = "Review code",
        BundlePath = bundlePath,
        Tools = []
    };

    [Fact]
    public async Task UploadAsync_NoExistingId_PostsToSkillsEndpoint()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":"sk_new","version":1}""", Encoding.UTF8, "application/json")
        });
        var uploader = MakeUploader(handler);

        var reference = await uploader.UploadAsync(MakeSkill(_bundleDir), existingSkillId: null);

        Assert.Equal("sk_new", reference.SkillId);
        Assert.Equal(BackendType.OpenAi, reference.Backend);
        Assert.Single(handler.Requests);
        Assert.Equal("https://api.openai.com/v1/skills", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
    }

    [Fact]
    public async Task UploadAsync_WithExistingId_PostsNewVersionAndSetsDefault()
    {
        var responses = new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"sk_existing","version":4}""", Encoding.UTF8, "application/json")
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"sk_existing","default_version":4}""", Encoding.UTF8, "application/json")
            }
        });
        var handler = new CapturingHandler(responses);
        var uploader = MakeUploader(handler);

        var reference = await uploader.UploadAsync(MakeSkill(_bundleDir), existingSkillId: "sk_existing");

        Assert.Equal("sk_existing", reference.SkillId);
        Assert.Equal("4", reference.Version);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://api.openai.com/v1/skills/sk_existing/versions", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("https://api.openai.com/v1/skills/sk_existing", handler.Requests[1].RequestUri!.ToString());
        Assert.Contains("\"default_version\":4", handler.RequestBodies[1]);
    }

    [Fact]
    public async Task DeleteAsync_IsNoOp_DoesNotIssueHttpCall()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var uploader = MakeUploader(handler);

        await uploader.DeleteAsync("sk_anything");

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ListAsync_IsNoOp_ReturnsEmptyDictionary()
    {
        var handler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var uploader = MakeUploader(handler);

        var result = await uploader.ListAsync();

        Assert.Empty(result);
        Assert.Empty(handler.Requests);
    }

    private static OpenAiSkillUploader MakeUploader(HttpMessageHandler handler)
    {
        var settings = new MaINSettings { OpenAiKey = "sk-test" };
        return new OpenAiSkillUploader(settings, new StubHttpClientFactory(handler));
    }
}

internal sealed class CapturingHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> RequestBodies { get; } = [];

    private readonly Queue<HttpResponseMessage> _responses;

    public CapturingHandler(HttpResponseMessage response)
    {
        _responses = new Queue<HttpResponseMessage>(new[] { response });
    }

    public CapturingHandler(Queue<HttpResponseMessage> responses)
    {
        _responses = responses;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_responses.Count == 0)
            return new HttpResponseMessage(HttpStatusCode.OK);

        return _responses.Dequeue();
    }
}

internal sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
