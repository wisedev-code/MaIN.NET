using MaIN.Domain.Entities.Skills;

namespace MaIN.Core.Hub.Skills;

public class RagExpertSkillProvider : IAgentSkillProvider
{
    public AgentSkill GetSkill() => new()
    {
        Name = "rag-expert",
        Description = "Enables knowledge-augmented answering via RAG.",
        Tags = ["rag", "knowledge", "retrieval"],
        Priority = 10,
        StepPlacement = SkillStepPlacement.Replace,
        Steps = ["ANSWER+USE_KNOWLEDGE"],
        InstructionFragment =
            "When answering, always ground your response in the provided knowledge context."
    };
}
