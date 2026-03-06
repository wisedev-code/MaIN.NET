using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Exceptions.Flows;
using MaIN.Infrastructure.Mappers;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;
using System.Text.Json;

namespace MaIN.Infrastructure.Repositories.FileSystem;

public class FileSystemAgentFlowRepository : IAgentFlowRepository
{
    private readonly string _directoryPath;
    private static readonly JsonSerializerOptions? JsonOptions = new() { WriteIndented = true };

    public FileSystemAgentFlowRepository(string basePath)
    {
        _directoryPath = Path.Combine(basePath, "flows");
        Directory.CreateDirectory(_directoryPath);
    }

    private string GetFilePath(string id) => Path.Combine(_directoryPath, $"{id}.json");

    public async Task<IEnumerable<AgentFlow>> GetAllFlows()
    {
        var files = Directory.GetFiles(_directoryPath, "*.json");
        var flows = new List<AgentFlow>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var doc = JsonSerializer.Deserialize<AgentFlowDocument>(json);
            if (doc is not null)
            {
                flows.Add(doc.ToDomain());
            }
        }

        return flows;
    }

    public async Task<AgentFlow?> GetFlowById(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<AgentFlowDocument>(json)?.ToDomain();
    }

    public async Task AddFlow(AgentFlow flow)
    {
        var filePath = GetFilePath(flow.Id!);
        if (File.Exists(filePath))
        {
            throw new FlowAlreadyExistsException(flow.Id!);
        }

        var json = JsonSerializer.Serialize(flow.ToDocument(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task UpdateFlow(string id, AgentFlow flow)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Flow with ID {id} not found.");
        }

        var json = JsonSerializer.Serialize(flow.ToDocument(), JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task DeleteFlow(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
        {
            throw new KeyNotFoundException($"Flow with ID {id} not found.");
        }

        await Task.Run(() => File.Delete(filePath));
    }
}
