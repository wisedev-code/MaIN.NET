using MaIN.Domain.Configuration;
using MaIN.Domain.Models.Abstract;

namespace MaIN.Domain.Models.Concrete;

public sealed class Gemma_2b : LocalModel
{
    public override string Id => "Gemma2-2B";
    public override string Name => "Gemma2-2B";
    public override string FileName => "Gemma2-2b.gguf";
    public override Uri DownloadUrl => new("https://huggingface.co/Inza124/gemma2_2b/resolve/main/gemma2-2b-maIN.gguf?download=true");
    public override string Description => "Compact 2B model for text generation, summarization, and simple Q&A";

    public override uint MaxContextWindowSize => 8192; // TODO: verify
}
