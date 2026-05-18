using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Domain.Entities.Skills;
using MaIN.Services.Services.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MaIN.Services.Services.Skills;

public class FileSystemSkillLoader(string directoryPath, ILogger<FileSystemSkillLoader>? logger = null) : ISkillLoader
{
    private const string FolderEntrypoint = "SKILL.md";

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(LowerCaseNamingConvention.Instance)
        .Build();

    private readonly ILogger _logger = logger ?? NullLogger<FileSystemSkillLoader>.Instance;

    public IReadOnlyList<AgentSkill> LoadAll()
    {
        var resolvedPath = Path.IsPathRooted(directoryPath)
            ? directoryPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, directoryPath));

        if (!Directory.Exists(resolvedPath))
            return [];

        var allMdFiles = Directory.GetFiles(resolvedPath, "*.md", SearchOption.AllDirectories);

        // Directories that contain SKILL.md are "skill packages".
        // Only SKILL.md is loaded from them — sibling files are includes, not standalone skills.
        var skillPackageDirs = allMdFiles
            .Where(f => Path.GetFileName(f).Equals(FolderEntrypoint, StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetDirectoryName(f)!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var filesToLoad = allMdFiles.Where(f =>
            Path.GetFileName(f).Equals(FolderEntrypoint, StringComparison.OrdinalIgnoreCase) ||
            !skillPackageDirs.Contains(Path.GetDirectoryName(f)!));

        return filesToLoad
            .Select(TryParseSkillFile)
            .OfType<AgentSkill>()
            .ToList()
            .AsReadOnly();
    }

    private AgentSkill? TryParseSkillFile(string filePath)
    {
        try
        {
            return ParseSkillFile(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to load skill file '{File}'. YAML frontmatter must use lowercase keys (name, priority, placement, etc.).",
                Path.GetFileName(filePath));
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

        var skillDir = Path.GetDirectoryName(filePath)!;
        var includesContent = LoadIncludes(dto.includes, skillDir);

        var fullFragment = (body, includesContent) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => body + "\n\n" + includesContent,
            ({ Length: > 0 }, _) => body,
            (_, { Length: > 0 }) => includesContent,
            _ => null
        };

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
            InstructionFragment = fullFragment
        };
    }

    private static string LoadIncludes(List<string>? includes, string baseDir)
    {
        if (includes is null || includes.Count == 0)
            return string.Empty;

        var parts = new List<string>();
        foreach (var include in includes)
        {
            foreach (var file in ResolveIncludePattern(include, baseDir))
            {
                var text = File.ReadAllText(file).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    parts.Add(text);
            }
        }

        return string.Join("\n\n", parts);
    }

    // Supports:
    //   "prompts/review.md"     — exact relative path
    //   "prompts/*.md"          — wildcard in last segment
    //   "examples/**"           — not supported, treated as literal (no recursion)
    private static IEnumerable<string> ResolveIncludePattern(string pattern, string baseDir)
    {
        var fullPattern = Path.GetFullPath(Path.Combine(baseDir, pattern));
        var dir = Path.GetDirectoryName(fullPattern) ?? baseDir;
        var filePattern = Path.GetFileName(fullPattern);

        if (!Directory.Exists(dir))
            yield break;

        foreach (var file in Directory.GetFiles(dir, filePattern, SearchOption.TopDirectoryOnly)
                     .OrderBy(f => f))
            yield return file;
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
    public List<string>? includes { get; set; }
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
