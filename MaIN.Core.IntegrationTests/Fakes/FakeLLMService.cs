using MaIN.Domain.Entities;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.LLMService;
using MaIN.Services.Services.Models;

namespace MaIN.Core.IntegrationTests.Fakes;

public sealed class FakeLLMService : ILLMService
{
    public Func<Chat, ChatResult?>? Handler { get; set; }

    public Task<ChatResult?> Send(Chat chat, ChatRequestOptions options, CancellationToken ct = default)
        => Task.FromResult(Handler?.Invoke(chat));

    public Task<ChatResult?> AskMemory(
        Chat chat,
        ChatMemoryOptions memOpts,
        ChatRequestOptions reqOpts,
        CancellationToken ct = default)
        => Task.FromResult(Handler?.Invoke(chat));

    public Task<string[]> GetCurrentModels() => Task.FromResult(Array.Empty<string>());

    public Task CleanSessionCache(string id) => Task.CompletedTask;
}
