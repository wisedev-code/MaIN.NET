using System.Text.Json;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.FileSystem;

public class FileSystemChatRepository : IChatRepository
{
    private readonly string _directoryPath;
    private static readonly JsonSerializerOptions? _jsonOptions = new() { WriteIndented = true };

    public FileSystemChatRepository(string basePath)
    {
        _directoryPath = Path.Combine(basePath, "chats");
        Directory.CreateDirectory(_directoryPath);
    }

    private string GetFilePath(string? id) => Path.Combine(_directoryPath, $"{id}.json");

    public async Task<IEnumerable<ChatDocument>> GetAllChats()
    {
        var files = Directory.GetFiles(_directoryPath, "*.json");
        var chats = new List<ChatDocument>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var chat = JsonSerializer.Deserialize<ChatDocument>(json);
            if (chat != null) chats.Add(chat);
        }

        return chats;
    }

    public async Task<ChatDocument?> GetChatById(string? id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath)) return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<ChatDocument>(json);
    }

    public async Task AddChat(ChatDocument chat)
    {
        var filePath = GetFilePath(chat.Id);
        if (File.Exists(filePath))
            throw new InvalidOperationException($"Chat with ID {chat.Id} already exists.");

        var json = JsonSerializer.Serialize(chat, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task UpdateChat(string? id, ChatDocument chat)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
            throw new KeyNotFoundException($"Chat with ID {id} not found.");

        var json = JsonSerializer.Serialize(chat, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task DeleteChat(string? id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
            throw new KeyNotFoundException($"Chat with ID {id} not found.");

        await Task.Run(() => File.Delete(filePath));
    }
}