using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models;

namespace Examples.Agents.Skills;

/// <summary>
/// Demonstrates a folder-based .md skill that wires up an MCP server.
/// The skill (skills/funfact-writer/SKILL.md) configures @modelcontextprotocol/server-filesystem
/// via its mcp: frontmatter — no C# required. The agent uses the MCP write_file tool
/// to create C:/Users/Public/funfacts/funfact.txt with a generated fun fact.
/// </summary>
public class AgentWithMcpFileWriterSkillExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("Agent with MCP file-writer skill (.md, OpenAi)");
        Console.WriteLine("Skill wires up @modelcontextprotocol/server-filesystem via SKILL.md frontmatter.");
        Console.WriteLine("Output: C:/Users/Public/funfacts/funfact.txt");

        OpenAiExample.Setup();

        var context = await AIHub.Agent()
            .WithModel(Models.OpenAi.Gpt4oMini)
            .WithSkill("funfact-writer")  // loaded from ./skills/funfact-writer/SKILL.md
            .CreateAsync();

        var result = await context.ProcessAsync("Generate a fun fact and save it to the file.");
        Console.WriteLine(result.Message.Content);
    }
}
