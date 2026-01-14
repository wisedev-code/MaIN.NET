using MaIN.Domain.Abstractions;
using MaIN.Domain.Configuration; // TODO: change localization

namespace MaIN.Domain.Models.Abstract;

public abstract class AIModel
{
    /// <summary> Internal Id. </summary>
    public abstract string Id { get; }

    /// <summary> Name displayed to users. </summary>
    public abstract string Name { get; }

    /// <summary> Gets the type of backend used by the model eg OpenAI or Self (Local). </summary>
    public abstract BackendType Backend { get; }

    /// <summary> System Message added before first prompt. </summary>
    public virtual string? SystemMessage { get; }

    /// <summary> Model description eg. capabilities or purpose. </summary>
    public virtual string? Description { get; }

    /// <summary> Max context widnow size supported by the model. </summary>
    public abstract uint MaxContextWindowSize { get; }

    public abstract T Accept<T>(IModelVisitor<T> visitor);
}

public abstract class LocalModel : AIModel
{
    /// <summary> Name of the model file on the hard drive eg. Gemma2-2b.gguf </summary>
    public abstract string FileName { get; }

    /// <summary> Uri to download model eg. https://huggingface.co/Inza124/gemma2_2b/resolve/main/gemma2-2b-maIN.gguf?download=true </summary>
    public abstract Uri DownloadUrl { get; }
    public override BackendType Backend => BackendType.Self;

    // Visitor Pattern
    public override T Accept<T>(IModelVisitor<T> visitor) => visitor.Visit(this);

    public bool IsDownloaded(string basePath)
        => File.Exists(Path.Combine(basePath, FileName));
}

public abstract class CloudModel : AIModel
{
    public abstract override BackendType Backend { get; }

    // Visitor Pattern
    public override T Accept<T>(IModelVisitor<T> visitor) => visitor.Visit(this);
}
