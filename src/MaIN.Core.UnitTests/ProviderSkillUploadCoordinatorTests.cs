using MaIN.Domain.Entities.Skills;
using MaIN.Services.Services.Skills;

namespace MaIN.Core.UnitTests;

public class ProviderSkillUploadCoordinatorTests
{
    [Fact]
    public void IsUploadable_BundleOnDisk_NoCodeTools_True()
    {
        var skill = new AgentSkill
        {
            Name = "code-review",
            BundlePath = "/some/path",
            Tools = []
        };
        Assert.True(ProviderSkillUploadCoordinator.IsUploadable(skill));
    }

    [Fact]
    public void IsUploadable_NoBundle_HasInstructionFragment_True()
    {
        // Code-defined skill with text content — can be synthesized into SKILL.md
        var skill = new AgentSkill
        {
            Name = "journalist",
            InstructionFragment = "Write like a journalist.",
            Tools = []
        };
        Assert.True(ProviderSkillUploadCoordinator.IsUploadable(skill));
    }

    [Fact]
    public void IsUploadable_NoBundle_HasDescriptionOnly_True()
    {
        var skill = new AgentSkill
        {
            Name = "summarizer",
            Description = "Summarises text",
            Tools = []
        };
        Assert.True(ProviderSkillUploadCoordinator.IsUploadable(skill));
    }

    [Fact]
    public void IsUploadable_ToolWithExecute_False()
    {
        // Code-backed tool — provider can't run a C# delegate server-side.
        var skill = new AgentSkill
        {
            Name = "web-search",
            BundlePath = "/some/path",
            Tools =
            [
                new SkillToolDefinition
                {
                    Name = "search",
                    Description = "Search the web",
                    Parameters = "{}",
                    Execute = async _ => "result"
                }
            ]
        };
        Assert.False(ProviderSkillUploadCoordinator.IsUploadable(skill));
    }

    [Fact]
    public void IsUploadable_NoBundle_NoContent_False()
    {
        var skill = new AgentSkill
        {
            Name = "empty",
            Tools = []
        };
        Assert.False(ProviderSkillUploadCoordinator.IsUploadable(skill));
    }
}
