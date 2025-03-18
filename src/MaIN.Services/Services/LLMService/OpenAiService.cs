using LLama.Common;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Dtos;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Utils;
using Microsoft.KernelMemory;

namespace MaIN.Services.Services.LLMService;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class OpenAiService(
    MaINSettings options,
    INotificationService notificationService)
    : ILLMService
{
    private readonly HttpClient _httpClient = new();
    private static readonly ConcurrentDictionary<string, List<ChatMessage>> SessionCache = new();
    
    public async Task<ChatResult?> Send(
        Chat chat,
        bool interactiveUpdates = false,
        bool createSession = false,
        Func<string?, Task>? changeOfValue = null)
    {
        if (string.IsNullOrEmpty(options.OpenAiKey) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            throw new Exception("OpenAI API key is not configured.");
        }

        if (chat == null || chat.Messages.Count == 0)
            return null;

        if (chat.Model == KnownModelNames.Llava_7b)
            throw new NotSupportedException("Image processing is not supported with ChatGPT models.");

        // Build the conversation history.
        List<ChatMessage> conversation;
        if (createSession)
        {
            conversation = SessionCache.GetOrAdd(chat.Id, new List<ChatMessage>());
            MergeMessages(conversation, chat.Messages);
        }
        else
        {
            conversation = new List<ChatMessage>();
            MergeMessages(conversation, chat.Messages.Take(chat.Messages.Count - 1).ToList());
        }
        
        var resultBuilder = new StringBuilder();

        var lastMessage = chat.Messages.Last();
        if (lastMessage.Files != null && lastMessage.Files.Count != 0)
        {
            var textData = lastMessage.Files!.Where(x => x.Content is not null)
                .ToDictionary(x => x.Name, x => x.Content);
            var fileData =
                lastMessage.Files!.Where(x => x.Path is not null)
                    .ToDictionary(x => x.Name, x => x.Path); //shity coode TODO
            var result = await AskMemory(chat, textData!, fileData!);
            resultBuilder.Append(result!.Message.Content);
        }
        else
        {
            if (interactiveUpdates || changeOfValue != null)
            {
                // Streaming mode.
                var requestBody = new
                {
                    model = chat.Model,
                    messages = conversation.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                    stream = true
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring("data: ".Length).Trim();
                        if (data == "[DONE]")
                            break;

                        try
                        {
                            var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data,
                                new JsonSerializerOptions()
                                {
                                    PropertyNameCaseInsensitive = true
                                });
                            var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                            if (!string.IsNullOrEmpty(content))
                            {
                                changeOfValue?.Invoke(content);
                                resultBuilder.Append(content);
                                await notificationService.DispatchNotification(
                                    NotificationMessageBuilder.CreateChatCompletion(chat.Id, content, false),
                                    "ReceiveMessageUpdate");
                            }
                        }
                        catch (JsonException)
                        {
                            Console.WriteLine($"Failed to parse chunk: {data}");
                        }
                    }
                }
            }
            else
            {
                var requestBody = new
                {
                    model = chat.Model,
                    messages = conversation.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                    stream = false
                };

                var requestJson = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);
                var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
                resultBuilder.Append(content);
            }
        }
        
        if (interactiveUpdates)
        {
            await notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateChatCompletion(chat.Id, resultBuilder.ToString(), true),
                "ReceiveMessageUpdate");
        }

        if (createSession)
        {
            if (SessionCache.TryGetValue(chat.Id, out var history))
            {
                history.Add(new ChatMessage("assistant", resultBuilder.ToString()));
            }
        }

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new MessageDto
            {
                Content = resultBuilder.ToString(),
                Role = AuthorRole.Assistant.ToString()
            }
        };
    }
    
    public async Task<ChatResult?> AskMemory(Chat chat,
        Dictionary<string, string>? textData = null,
        Dictionary<string, string>? fileData = null,
        List<string>? webUrls = null,
        List<string>? memory = null)
    {
        if (chat == null || chat.Messages == null || !chat.Messages.Any())
            return null;

        var kernelMemory = new KernelMemoryBuilder()
            .WithOpenAIDefaults((options.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")) ?? string.Empty) 
            .Build();

        if (textData != null)
        {
            foreach (var item in textData)
            {
                await kernelMemory.ImportTextAsync(item.Value, item.Key);
            }
        }

        if (fileData != null)
        {
            foreach (var item in fileData)
            {
                try
                {
                    await kernelMemory.ImportDocumentAsync(item.Value, item.Key);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error importing file '{item.Key}': {ex.Message}");
                }
            }
        }
        
        if (webUrls != null)
        {
            foreach (var url in webUrls)
            {
                await kernelMemory.ImportWebPageAsync(url);
            }
        }

        if (memory != null)
        {
            for (int i = 0; i < memory.Count; i++)
            {
                await kernelMemory.ImportTextAsync(memory[i], $"memory_{i + 1}");
            }
        }

        var userQuery = chat.Messages.Last().Content;
        var retrievedContext = await kernelMemory.AskAsync(userQuery);

        await kernelMemory.DeleteIndexAsync();

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new MessageDto
            {
                Content = retrievedContext.Result,
                Role = AuthorRole.Assistant.ToString()
            }
        };
    }
    
    public async Task<List<string?>> GetCurrentModels()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.OpenAiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var modelsResponse = JsonSerializer.Deserialize<OpenAiModelsResponse>(responseJson);

        List<string?> models = modelsResponse?.Data?
            .Where(m => m.Id!.StartsWith("gpt-", StringComparison.InvariantCultureIgnoreCase))
            .Select(m => m.Id)
            .ToList() ?? new List<string?>();

        return models;
    }

    public Task CleanSessionCache(string id)
    {
        SessionCache.TryRemove(id, out _);
        return Task.CompletedTask;
    }
    
    private void MergeMessages(List<ChatMessage> conversation, List<Message> messages)
    {
        var existing = new HashSet<(string, string)>(conversation.Select(m => (m.Role, m.Content)));
        foreach (var msg in messages)
        {
            var role = msg.Role.ToLowerInvariant();
            if (!existing.Contains((role, msg.Content)))
            {
                conversation.Add(new ChatMessage(role, msg.Content));
                existing.Add((role, msg.Content));
            }
        }
    }
}

public class ChatMessage
{
    public string Role { get; set; }
    public string Content { get; set; }

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

public class ChatCompletionResponse
{
    public List<Choice>? Choices { get; set; }
}

public class Choice
{
    public ChatMessageResponse? Message { get; set; }
}

public class ChatMessageResponse
{
    public string? Content { get; set; }
}

public class ChatCompletionChunk
{
    public List<ChoiceChunk>? Choices { get; set; }
}

public class ChoiceChunk
{
    public Delta? Delta { get; set; }
}

public class Delta
{
    public string? Content { get; set; }
}

public class OpenAiModelsResponse
{
    public List<OpenAiModel>? Data { get; set; }
}

public abstract class OpenAiModel
{
    public string? Id { get; set; }
}