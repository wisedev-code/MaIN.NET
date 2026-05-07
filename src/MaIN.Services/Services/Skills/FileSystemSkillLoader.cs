using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Skills;
using MaIN.Services.Services.Abstract;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MaIN.Services.Services.Skills;

public class FileSystemSkillLoader(string directoryPath) : ISkillLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(LowerCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public IReadOnlyList<AgentSkill> LoadAll()
    {
        var resolvedPath = Path.IsPathRooted(directoryPath)
            ? directoryPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, directoryPath));

        if (!Directory.Exists(resolvedPath))
            return [];

        return Directory
            .GetFiles(resolvedPath, "*.md", SearchOption.AllDirectories)
            .Select(TryParseSkillFile)
            .OfType<AgentSkill>()
            .ToList()
            .AsReadOnly();
    }

    private static AgentSkill? TryParseSkillFile(string filePath)
    {
        try
        {
            return ParseSkillFile(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Skills] Failed to load '{Path.GetFileName(filePath)}': {ex.Message}");
            return null;
        }
    }

    private static AgentSkill? ParseSkillFile(string filePath)
    {
        var content = File.ReadAllText(filePath);

        if (!content.TrimStart().StartsWith("---"))
            return null;

        var parts = content.Split(["---"], 3, StringSplitOptions.None);
        if (parts.Length < 3)
            return null;

        var frontmatter = parts[1].Trim();
        var body = parts[2].Trim();

        var dto = Deserializer.Deserialize<SkillFileDto>(frontmatter);
        if (string.IsNullOrWhiteSpace(dto?.name))
            return null;

        return new AgentSkill
        {
            Name = dto.name,
            Description = dto.description,
            Version = dto.version ?? "1.0.0",
            Steps = dto.steps ?? [],
            Priority = dto.priority,
            Tags = (dto.tags ?? []).ToArray(),
            StepPlacement = ParsePlacement(dto.placement),
            Behaviours = dto.behaviours ?? [],
            Source = BuildSource(dto.source),
            Mcp = BuildMcp(dto.mcp),
            InstructionFragment = string.IsNullOrWhiteSpace(body) ? null : body
        };
    }

    private static SkillStepPlacement ParsePlacement(string? placement) =>
        placement?.ToLowerInvariant() switch
        {
            "after" => SkillStepPlacement.After,
            "replace" => SkillStepPlacement.Replace,
            _ => SkillStepPlacement.Before
        };

    private static SkillSourceDefinition? BuildSource(SkillFileSourceDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.type)) return null;

        return dto.type.ToUpperInvariant() switch
        {
            "WEB" when !string.IsNullOrWhiteSpace(dto.url) =>
                new SkillSourceDefinition
                {
                    Details = new AgentWebSourceDetails { Url = dto.url },
                    Type = AgentSourceType.Web
                },
            "FILE" when !string.IsNullOrWhiteSpace(dto.url) =>
                new SkillSourceDefinition
                {
                    Details = new AgentFileSourceDetails { Files = [dto.url] },
                    Type = AgentSourceType.File
                },
            _ => null
        };
    }

    private static SkillMcpDefinition? BuildMcp(SkillFileMcpDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.command)) return null;

        return new SkillMcpDefinition
        {
            Command = dto.command,
            Arguments = dto.arguments ?? [],
            Environment = dto.environment ?? [],
            Properties = dto.properties ?? []
        };
    }
}

internal class SkillFileDto
{
    public string name { get; set; } = "";
    public string? description { get; set; }
    public string? version { get; set; }
    public List<string>? steps { get; set; }
    public string? placement { get; set; }
    public int priority { get; set; } = 100;
    public List<string>? tags { get; set; }
    public Dictionary<string, string>? behaviours { get; set; }
    public SkillFileSourceDto? source { get; set; }
    public SkillFileMcpDto? mcp { get; set; }
}

internal class SkillFileSourceDto
{
    public string type { get; set; } = "";
    public string? url { get; set; }
    public string? path { get; set; }
}

internal class SkillFileMcpDto
{
    public string command { get; set; } = "";
    public List<string>? arguments { get; set; }
    public Dictionary<string, string>? environment { get; set; }
    public Dictionary<string, string>? properties { get; set; }
}
