using LLama;
using LLama.Common;
using LLamaSharp.SemanticKernel.ChatCompletion;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Models;
using MaIN.Services.Models.Ollama;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using AuthorRole = Microsoft.SemanticKernel.ChatCompletion.AuthorRole;

namespace MaIN.Services.Services;

public class LLMService(IOptions<MaINSettings> options) : ILLMService
{
    public async Task<ChatResult?> Send(Chat? chat)
    {
        var path = options.Value.ModelsPath;
        var model = KnownModels.GetModel(path, chat!.Model);
        
        var parameters = new ModelParams(Path.Combine(path, model.FileName))
        {
            ContextSize = 1024, // The longest length of chat as memory.
            GpuLayerCount = 25// How many layers to offload to GPU. Please adjust it according to your GPU memory.
        };

        using var llmModel = await LLamaWeights.LoadFromFileAsync(parameters);
        var ex = new StatelessExecutor(llmModel, parameters);

        var completion = new LLamaSharpChatCompletion(ex);
        var history = chat.Type == ChatType.Conversation ? completion.CreateNewChat() : completion.CreateNewChat(chat.Messages?.First().Content);
        foreach (var message in chat.Messages!)
        {
            if(history.Any(x => x.Role.Label == message.Role && x.Content == message.Content)) continue;
            history.AddMessage(new AuthorRole(message.Role), message.Content);
        }

        var result = await completion.GetChatMessageContentAsync(history);
        var chatResult = new ChatResult()
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new MessageDto()
            {
                Content = result.Content!,
                Role = result.Role.Label
            }
        };
        
        return chatResult;
    }

    public Task<List<string>> GetCurrentModels()
    {
        var path = options.Value.ModelsPath;
        var files = Directory.GetFiles(path, "*.gguf", SearchOption.AllDirectories).ToList();

        return Task.FromResult(files.Select(x => KnownModels.GetModelByFileName(path, x).Name).ToList());
    }
}