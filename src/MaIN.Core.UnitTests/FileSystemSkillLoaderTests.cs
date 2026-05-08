using MaIN.Domain.Entities.Skills;
using MaIN.Services.Services.Skills;

namespace MaIN.Core.UnitTests;

public class FileSystemSkillLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"skill-tests-{Guid.NewGuid()}");

    public FileSystemSkillLoaderTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private void WriteFile(string relativePath, string content)
    {
        var full = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    // --- Directory ---

    [Fact]
    public void LoadAll_NonexistentDirectory_ReturnsEmpty()
    {
        var loader = new FileSystemSkillLoader(Path.Combine(_tempDir, "does-not-exist"));
        Assert.Empty(loader.LoadAll());
    }

    // --- Parsing ---

    [Fact]
    public void LoadAll_ValidSkill_ParsedCorrectly()
    {
        WriteFile("web-search.md", """
            ---
            name: web-search
            description: Search the web
            version: 2.0.0
            steps:
              - FETCH_DATA
              - ANSWER
            placement: before
            priority: 10
            tags:
              - web
              - search
            ---

            Search the web and provide sourced answers.
            """);

        var skills = new FileSystemSkillLoader(_tempDir).LoadAll();

        Assert.Single(skills);
        var s = skills[0];
        Assert.Equal("web-search", s.Name);
        Assert.Equal("Search the web", s.Description);
        Assert.Equal("2.0.0", s.Version);
        Assert.Equal(["FETCH_DATA", "ANSWER"], s.Steps);
        Assert.Equal(SkillStepPlacement.Before, s.StepPlacement);
        Assert.Equal(10, s.Priority);
        Assert.Contains("web", s.Tags);
        Assert.Contains("search", s.Tags);
        Assert.Contains("Search the web and provide sourced answers.", s.InstructionFragment);
    }

    [Fact]
    public void LoadAll_NoFrontmatter_SkipsFile()
    {
        WriteFile("no-front.md", "Just markdown without frontmatter.");
        Assert.Empty(new FileSystemSkillLoader(_tempDir).LoadAll());
    }

    [Fact]
    public void LoadAll_MissingName_SkipsFile()
    {
        WriteFile("no-name.md", """
            ---
            description: No name here
            steps:
              - ANSWER
            ---
            Do something.
            """);

        Assert.Empty(new FileSystemSkillLoader(_tempDir).LoadAll());
    }

    [Fact]
    public void LoadAll_DefaultPriority_Is100()
    {
        WriteFile("default.md", """
            ---
            name: default-priority
            ---
            """);

        var skills = new FileSystemSkillLoader(_tempDir).LoadAll();

        Assert.Equal(100, skills[0].Priority);
    }

    [Fact]
    public void LoadAll_PlacementReplace_ParsedCorrectly()
    {
        WriteFile("replace.md", """
            ---
            name: replace-skill
            placement: replace
            steps:
              - MCP
            ---
            """);

        var skills = new FileSystemSkillLoader(_tempDir).LoadAll();

        Assert.Equal(SkillStepPlacement.Replace, skills[0].StepPlacement);
    }

    [Fact]
    public void LoadAll_PlacementAfter_ParsedCorrectly()
    {
        WriteFile("after.md", """
            ---
            name: after-skill
            placement: after
            steps:
              - SUMMARIZE
            ---
            """);

        var skills = new FileSystemSkillLoader(_tempDir).LoadAll();

        Assert.Equal(SkillStepPlacement.After, skills[0].StepPlacement);
    }

    [Fact]
    public void LoadAll_UnknownPlacement_DefaultsBefore()
    {
        WriteFile("unknown-placement.md", """
            ---
            name: unknown-placement-skill
            placement: sideways
            ---
            """);

        var skills = new FileSystemSkillLoader(_tempDir).LoadAll();

        Assert.Equal(SkillStepPlacement.Before, skills[0].StepPlacement);
    }

    // --- MCP ---

    [Fact]
    public void LoadAll_WithMcpBlock_ParsesMcpDefinition()
    {
        WriteFile("mcp-skill.md", """
            ---
            name: fs-tools
            steps:
              - MCP
            placement: replace
            mcp:
              command: npx
              arguments:
                - -y
                - "@modelcontextprotocol/server-filesystem"
                - /tmp
            ---
            Use filesystem tools.
            """);

        var skills = new FileSystemSkillLoader(_tempDir).LoadAll();

        Assert.Single(skills);
        var mcp = skills[0].Mcp;
        Assert.NotNull(mcp);
        Assert.Equal("npx", mcp!.Command);
        Assert.Equal(["-y", "@modelcontextprotocol/server-filesystem", "/tmp"], mcp.Arguments);
    }

    // --- Folder-based skills ---

    [Fact]
    public void LoadAll_FolderSkill_LoadsOnlySkillMd()
    {
        WriteFile("my-skill/SKILL.md", """
            ---
            name: folder-skill
            steps:
              - ANSWER
            ---
            Main skill prompt.
            """);

        // Sibling file in same folder — must NOT be loaded as separate skill
        WriteFile("my-skill/helper.md", """
            ---
            name: should-be-skipped
            steps:
              - ANSWER
            ---
            Helper content.
            """);

        var skills = new FileSystemSkillLoader(_tempDir).LoadAll();

        Assert.Single(skills);
        Assert.Equal("folder-skill", skills[0].Name);
    }

    [Fact]
    public void LoadAll_MultipleTopLevelSkills_LoadsAll()
    {
        WriteFile("skill-a.md", "---\nname: skill-a\n---\n");
        WriteFile("skill-b.md", "---\nname: skill-b\n---\n");
        WriteFile("skill-c.md", "---\nname: skill-c\n---\n");

        var skills = new FileSystemSkillLoader(_tempDir).LoadAll();

        Assert.Equal(3, skills.Count);
    }
}
