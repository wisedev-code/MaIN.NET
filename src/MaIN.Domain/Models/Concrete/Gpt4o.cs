using MaIN.Domain.Configuration;
using MaIN.Domain.Models.Abstract;

namespace MaIN.Domain.Models.Concrete;

public sealed record Gpt4o : CloudModel
{
    public override string Id => "gpt-4o";
    public override string Name => "GPT-4 Omni";
    public override BackendType Backend => BackendType.OpenAi;
    public override uint MaxContextWindowSize => 128000; // TODO: verify
    public override string Description => "Most advanced OpenAI model.";
}
