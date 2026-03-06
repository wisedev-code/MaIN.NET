using MaIN.Domain.Entities.Agents;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using System.Text.Json;

namespace MaIN.Infrastructure.Repositories.FileSystem;

public class FileSystemAgentRepository : IAgentRepository
{
    private readonly string _directoryPath;
    private static readonly JsonSerializerOptions? JsonOptions = new() { WriteIndented = true };

    public FileSystemAgentRepository(string basePath)
    {
        _directoryPath = Path.Combine(basePath, "agents");
        Directory.CreateDirectory(_directoryPath);
    }

    private string GetFilePath(string id) => Path.Combine(_directoryPath, $"{id}.json");

    public async Task<IEnumerable<Agent>> GetAllAgents()
    {
        var files = Directory.GetFiles(_directoryPath, "*.json");
        var agents = new List<Agent>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var doc = JsonSerializer.Deserialize<AgentDocument>(json);
            if (doc is not null)
            {
                agents.Add(doc.ToDomain());
            }
        }

        return agents;
    }

    public async Task<Agent?> GetAgentById(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<AgentDocument>(json)?.ToDomain();
    }

    public async Task AddAgent(Agent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var filePath = GetFilePath(agent.Id);
        if (File.Exists(filePath))
        {
            throw new AgentAlreadyExistsException(agent.Id);
        }

        var json = JsonSerializer.Serialize(agent.ToDocument(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task UpdateAgent(string id, Agent agent)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Agent with ID {id} not found.");
        }

        var json = JsonSerializer.Serialize(agent.ToDocument(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task DeleteAgent(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Agent with ID {id} not found.");
        }

        await Task.Run(() => File.Delete(filePath));
    }

    public async Task<bool> Exists(string id) =>
        await Task.FromResult(File.Exists(GetFilePath(id)));
}
