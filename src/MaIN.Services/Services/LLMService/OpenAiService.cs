using LLama.Common;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Models;
using MaIN.Services.Models;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Utils;

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
using Microsoft.Extensions.Options;

public class OpenAiService(
    IOptions<MaINSettings> options,
    INotificationService notificationService)
    : ILLMService
{
    private readonly HttpClient httpClient = new();
    // Session cache for conversation history when createSession is true.
    private static readonly ConcurrentDictionary<string, List<ChatMessage>> sessionCache = new();

    /// <summary>
    /// Sends a chat message using OpenAI’s chat completions API.
    /// Supports streaming responses and conversation history caching.
    /// </summary>
    public async Task<ChatResult?> Send(Chat? chat, bool interactiveUpdates = false, bool createSession = false)
    {
        if (string.IsNullOrEmpty(options.Value.OpenAiKey))
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
            conversation = sessionCache.GetOrAdd(chat.Id, new List<ChatMessage>());
            MergeMessages(conversation, chat.Messages);
        }
        else
        {
            conversation = new List<ChatMessage>();
            MergeMessages(conversation, chat.Messages.Take(chat.Messages.Count - 1).ToList());
        }

        // Append the current user message.
        var userMsg = chat.Messages.Last();
        conversation.Add(new ChatMessage("user", userMsg.Content));

        var resultBuilder = new StringBuilder();

        if (interactiveUpdates)
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
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.OpenAiKey);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
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
                        var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data);
                        var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
                        if (!string.IsNullOrEmpty(content))
                        {
                            resultBuilder.Append(content);
                            await notificationService.DispatchNotification(
                                NotificationMessageBuilder.CreateChatCompletion(chat.Id, resultBuilder.ToString(), false),
                                "ReceiveMessageUpdate");
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed JSON chunks.
                    }
                }
            }
        }
        else
        {
            // Non-streaming mode.
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
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.OpenAiKey);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);
            var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
            resultBuilder.Append(content);
        }

        if (interactiveUpdates)
        {
            await notificationService.DispatchNotification(
                NotificationMessageBuilder.CreateChatCompletion(chat.Id, resultBuilder.ToString(), true),
                "ReceiveMessageUpdate");
        }

        // Cache assistant response if session mode is enabled.
        if (createSession)
        {
            if (sessionCache.TryGetValue(chat.Id, out var history))
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

    /// <summary>
    /// Production-quality AskMemory:
    /// Uses OpenAI embeddings to index provided texts, files, and additional memory,
    /// then performs a similarity search and injects the retrieved context into a ChatGPT prompt.
    /// </summary>
    public async Task<ChatResult?> AskMemory(Chat? chat,
        Dictionary<string, string>? textData = null,
        Dictionary<string, string>? fileData = null,
        List<string>? memory = null)
    {
        if (chat == null || chat.Messages == null || !chat.Messages.Any())
            return null;

        // Create a new KernelMemory instance (could be replaced with a persistent store).
        using var kernelMemory = new KernelMemory(options.Value.OpenAiKey);

        // Import text snippets.
        if (textData != null)
        {
            foreach (var item in textData)
            {
                await kernelMemory.ImportTextAsync(item.Value, item.Key);
            }
        }
        // Import files.
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
                    // Log or handle file-read errors as needed.
                    await kernelMemory.ImportTextAsync($"[Error reading file: {ex.Message}]", item.Key);
                }
            }
        }
        // Import additional memory strings.
        if (memory != null)
        {
            for (int i = 0; i < memory.Count; i++)
            {
                await kernelMemory.ImportTextAsync(memory[i], $"memory_{i + 1}");
            }
        }

        // Retrieve the most relevant context for the user's question.
        var userQuery = chat.Messages.Last().Content;
        var retrievedContext = await kernelMemory.AskAsync(userQuery);

        // Build a new prompt that injects the retrieved context.
        var messages = new List<ChatMessage>
        {
            new ChatMessage("system", $"The following context was retrieved from memory and may be relevant:\n{retrievedContext}"),
            new ChatMessage("user", userQuery)
        };

        var requestBody = new
        {
            model = chat.Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            stream = false
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.OpenAiKey);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);
        var contentResponse = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        // Optionally clear the memory index if you want a fresh slate per request.
        await kernelMemory.DeleteIndexAsync();

        return new ChatResult
        {
            Done = true,
            CreatedAt = DateTime.Now,
            Model = chat.Model,
            Message = new MessageDto
            {
                Content = contentResponse,
                Role = AuthorRole.Assistant.ToString()
            }
        };
    }

    /// <summary>
    /// Calls the OpenAI models API to list available models.
    /// </summary>
    public async Task<List<string>> GetCurrentModels()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.OpenAiKey);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var modelsResponse = JsonSerializer.Deserialize<OpenAiModelsResponse>(responseJson);

        var models = modelsResponse?.Data?
            .Where(m => m.Id.StartsWith("gpt-", StringComparison.InvariantCultureIgnoreCase))
            .Select(m => m.Id)
            .ToList() ?? new List<string>();

        return models;
    }

    public Task CleanSessionCache(string id)
    {
        sessionCache.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Merges messages into an existing conversation while avoiding duplicates.
    /// </summary>
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

/// <summary>
/// Represents a ChatGPT conversation message.
/// </summary>
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

/// <summary>
/// Models the response from OpenAI’s chat completions endpoint.
/// </summary>
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

/// <summary>
/// Models a streaming chunk from OpenAI’s chat completions endpoint.
/// </summary>
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

/// <summary>
/// Models the response from the OpenAI models API.
/// </summary>
public class OpenAiModelsResponse
{
    public List<OpenAiModel>? Data { get; set; }
}

public class OpenAiModel
{
    public string Id { get; set; }
}

/// <summary>
/// A production-style memory component that uses OpenAI embeddings to store and retrieve text.
/// </summary>
public class KernelMemory : IDisposable
{
    private readonly string apiKey;
    private readonly HttpClient httpClient;
    private readonly List<MemoryItem> memoryStore = new();

    public KernelMemory(string apiKey)
    {
        this.apiKey = apiKey;
        this.httpClient = new HttpClient();
    }

    /// <summary>
    /// Imports raw text into the memory index.
    /// </summary>
    public async Task ImportTextAsync(string text, string key)
    {
        var embedding = await ComputeEmbeddingAsync(text);
        memoryStore.Add(new MemoryItem { Id = key, Content = text, Embedding = embedding });
    }

    /// <summary>
    /// Imports a document by reading its file content.
    /// </summary>
    public async Task ImportDocumentAsync(string filePath, string key)
    {
        var text = await File.ReadAllTextAsync(filePath);
        await ImportTextAsync(text, key);
    }

    /// <summary>
    /// Given a query, computes its embedding and returns the concatenated content
    /// of the top matching memory items.
    /// </summary>
    public async Task<string> AskAsync(string query, int topK = 3)
    {
        var queryEmbedding = await ComputeEmbeddingAsync(query);
        var matches = memoryStore
            .Select(mi => new { Item = mi, Score = CosineSimilarity(queryEmbedding, mi.Embedding) })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        var sb = new StringBuilder();
        foreach (var match in matches)
        {
            sb.AppendLine(match.Item.Content);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Clears the memory index.
    /// </summary>
    public Task DeleteIndexAsync()
    {
        memoryStore.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Calls OpenAI’s embeddings API to compute an embedding for the given text.
    /// </summary>
    private async Task<float[]> ComputeEmbeddingAsync(string text)
    {
        var requestBody = new
        {
            input = text,
            model = "text-embedding-ada-002"
        };
        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var embeddingResponse = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(responseJson);
        var embeddingList = embeddingResponse?.Data?.FirstOrDefault()?.Embedding;
        return embeddingList != null ? embeddingList.ToArray() : Array.Empty<float>();
    }

    /// <summary>
    /// Computes the cosine similarity between two vectors.
    /// </summary>
    private float CosineSimilarity(float[] v1, float[] v2)
    {
        if (v1.Length != v2.Length || v1.Length == 0) return 0;
        float dot = 0, norm1 = 0, norm2 = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            norm1 += v1[i] * v1[i];
            norm2 += v2[i] * v2[i];
        }
        if (norm1 == 0 || norm2 == 0) return 0;
        return dot / ((float)Math.Sqrt(norm1) * (float)Math.Sqrt(norm2));
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}

/// <summary>
/// Represents a memory item in the KernelMemory store.
/// </summary>
public class MemoryItem
{
    public string Id { get; set; }
    public string Content { get; set; }
    public float[] Embedding { get; set; }
}

/// <summary>
/// Models the response from OpenAI’s embeddings API.
/// </summary>
public class OpenAiEmbeddingResponse
{
    public List<EmbeddingData> Data { get; set; }
}

public class EmbeddingData
{
    public List<float> Embedding { get; set; }
}
