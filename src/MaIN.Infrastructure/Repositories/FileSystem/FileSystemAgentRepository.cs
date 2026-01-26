using System.Text.Json;
using MaIN.Domain.Exceptions.Agents;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

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

    public async Task<IEnumerable<AgentDocument>> GetAllAgents()
    {
        var files = Directory.GetFiles(_directoryPath, "*.json");
        var agents = new List<AgentDocument>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var agent = JsonSerializer.Deserialize<AgentDocument>(json);
            if (agent != null) agents.Add(agent);
        }

        return agents;
    }

    public async Task<AgentDocument?> GetAgentById(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath)) return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<AgentDocument>(json);
    }

    public async Task AddAgent(AgentDocument agent)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var filePath = GetFilePath(agent.Id);
        if (File.Exists(filePath))
            throw new AgentAlreadyExistsException(agent.Id);

        var json = JsonSerializer.Serialize(agent, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task UpdateAgent(string id, AgentDocument agent)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
            throw new KeyNotFoundException($"Agent with ID {id} not found.");

        var json = JsonSerializer.Serialize(agent, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task DeleteAgent(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
            throw new KeyNotFoundException($"Agent with ID {id} not found.");

        await Task.Run(() => File.Delete(filePath));
    }

    public async Task<bool> Exists(string id) =>
        await Task.FromResult(File.Exists(GetFilePath(id)));
}