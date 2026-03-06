using MaIN.Domain.Entities;
using MaIN.Domain.Exceptions.Chats;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using System.Text.Json;

namespace MaIN.Infrastructure.Repositories.FileSystem;

public class FileSystemChatRepository : IChatRepository
{
    private readonly string _directoryPath;
    private static readonly JsonSerializerOptions? JsonOptions = new() { WriteIndented = true };

    public FileSystemChatRepository(string basePath)
    {
        _directoryPath = Path.Combine(basePath, "chats");
        Directory.CreateDirectory(_directoryPath);
    }

    private string GetFilePath(string id) => Path.Combine(_directoryPath, $"{id}.json");

    public async Task<IEnumerable<Chat>> GetAllChats()
    {
        var files = Directory.GetFiles(_directoryPath, "*.json");
        var chats = new List<Chat>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var doc = JsonSerializer.Deserialize<ChatDocument>(json);
            if (doc is not null)
            {
                chats.Add(doc.ToDomain());
            }
        }

        return chats;
    }

    public async Task<Chat?> GetChatById(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<ChatDocument>(json)?.ToDomain();
    }

    public async Task AddChat(Chat chat)
    {
        var filePath = GetFilePath(chat.Id);
        if (File.Exists(filePath))
        {
            throw new ChatAlreadyExistsException(chat.Id);
        }

        var json = JsonSerializer.Serialize(chat.ToDocument(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task UpdateChat(string id, Chat chat)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Chat with ID {id} not found.");
        }

        var json = JsonSerializer.Serialize(chat.ToDocument(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task DeleteChat(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Chat with ID {id} not found.");
        }

        await Task.Run(() => File.Delete(filePath));
    }
}
