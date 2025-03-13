using System.Text.Json;
using MaIN.Infrastructure.Models;
using MaIN.Infrastructure.Repositories.Abstract;

namespace MaIN.Infrastructure.Repositories.FileSystem;

public class FileSystemAgentFlowRepository : IAgentFlowRepository
{
    private readonly string _directoryPath;
    private static readonly JsonSerializerOptions? _jsonOptions = new() { WriteIndented = true };

    public FileSystemAgentFlowRepository(string basePath)
    {
        _directoryPath = Path.Combine(basePath, "flows");
        Directory.CreateDirectory(_directoryPath);
    }

    private string GetFilePath(string id) => Path.Combine(_directoryPath, $"{id}.json");

    public async Task<IEnumerable<AgentFlowDocument>> GetAllFlows()
    {
        var files = Directory.GetFiles(_directoryPath, "*.json");
        var flows = new List<AgentFlowDocument>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var flow = JsonSerializer.Deserialize<AgentFlowDocument>(json);
            if (flow != null) flows.Add(flow);
        }

        return flows;
    }

    public async Task<AgentFlowDocument?> GetFlowById(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath)) return null;

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<AgentFlowDocument>(json);
    }

    public async Task AddFlow(AgentFlowDocument flow)
    {
        var filePath = GetFilePath(flow.Id);
        if (File.Exists(filePath))
            throw new InvalidOperationException($"Flow with ID {flow.Id} already exists.");

        var json = JsonSerializer.Serialize(flow, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task UpdateFlow(string id, AgentFlowDocument flow)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
            throw new KeyNotFoundException($"Flow with ID {id} not found.");

        var json = JsonSerializer.Serialize(flow, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task DeleteFlow(string id)
    {
        var filePath = GetFilePath(id);
        if (!File.Exists(filePath))
            throw new KeyNotFoundException($"Flow with ID {id} not found.");

        await Task.Run(() => File.Delete(filePath));
    }
}
