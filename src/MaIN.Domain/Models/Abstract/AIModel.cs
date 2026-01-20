using MaIN.Domain.Configuration;

namespace MaIN.Domain.Models.Abstract;

public abstract record AIModel(
    string Id,
    BackendType Backend,
    string? Name = null,
    uint MaxContextWindowSize = 128000,
    string? Description = null,
    string? SystemMessage = null)
{
    /// <summary> Internal Id. For cloud models it is the cloud Id. </summary>
    public virtual string Id { get; } = Id;

    /// <summary> Name displayed to users. </summary>
    public virtual string Name { get; } = Name ?? Id;

    /// <summary> Gets the type of backend used by the model eg OpenAI or Self (Local). </summary>
    public virtual BackendType Backend { get; } = Backend;

    /// <summary> System Message added before first prompt. </summary>
    public virtual string? SystemMessage { get; } = SystemMessage;

    /// <summary> Model description eg. capabilities or purpose. </summary>
    public virtual string? Description { get; } = Description;

    /// <summary> Max context window size supported by the model. </summary>
    public virtual uint MaxContextWindowSize { get; } = MaxContextWindowSize;

    /// <summary> Checks if model supports reasoning/thinking mode. </summary>
    public bool HasReasoning => this is IReasoningModel;
    
    /// <summary> Checks if model supports vision/image input. </summary>
    public bool HasVision => this is IVisionModel;
}

/// <summary> Base class for local models. </summary>
public abstract record LocalModel(
    string Id,
    string FileName,
    Uri? DownloadUrl = null,
    string? Name = null,
    uint MaxContextWindowSize = 128000,
    string? Description = null,
    string? SystemMessage = null) : AIModel(Id, BackendType.Self, Name, MaxContextWindowSize, Description, SystemMessage)
{
    /// <summary> Name of the model file on the hard drive eg. Gemma2-2b.gguf </summary>
    public virtual string FileName { get; } = FileName;

    /// <summary> Uri to download model eg. https://huggingface.co/Inza124/gemma2_2b/resolve/main/gemma2-2b-maIN.gguf?download=true </summary>
    public virtual Uri? DownloadUrl { get; } = DownloadUrl;

    public virtual bool IsDownloaded(string basePath)
        => File.Exists(Path.Combine(basePath, FileName));
        
    public virtual string GetFullPath(string basePath)
        => Path.Combine(basePath, FileName);
}

/// <summary> Base class for cloud models. </summary>
public abstract record CloudModel(
    string Id,
    BackendType Backend,
    string? Name = null,
    uint MaxContextWindowSize = 128000,
    string? Description = null,
    string? SystemMessage = null) : AIModel(Id, Backend, Name, MaxContextWindowSize, Description, SystemMessage)
{
}

/// <summary> Generic class for runtime defined cloud models. </summary>
public record GenericCloudModel(
    string Id,
    BackendType Backend,
    string? Name = null,
    uint MaxContextWindowSize = 128000,
    string? Description = null,
    string? SystemMessage = null
) : CloudModel(Id, Backend, Name, MaxContextWindowSize, Description, SystemMessage)
{
}

/// <summary> Generic class for runtime defined cloud models with reasoning capability. </summary>
public record GenericCloudReasoningModel(
    string Id,
    BackendType Backend,
    string? Name = null,
    uint MaxContextWindowSize = 128000,
    string? Description = null,
    string? SystemMessage = null,
    string? AdditionalPrompt = null
) : CloudModel(Id, Backend, Name, MaxContextWindowSize, Description, SystemMessage), IReasoningModel
{   
    // IReasoningModel - null for cloud (handled by provider API)
    public Func<string, Models.ThinkingState, Models.LLMTokenValue>? ReasonFunction => null;
    public string? AdditionalPrompt { get; } = AdditionalPrompt;
}

/// <summary> Generic class for runtime defined cloud models with vision capability. </summary>
public record GenericCloudVisionModel(
    string Id,
    BackendType Backend,
    string? Name = null,
    uint MaxContextWindowSize = 128000,
    string? Description = null,
    string? SystemMessage = null
) : CloudModel(Id, Backend, Name, MaxContextWindowSize, Description, SystemMessage), IVisionModel
{   
    // IVisionModel - cloud models don't need MMProjectPath
    public string? MMProjectPath => null;
}

/// <summary> Generic class for runtime defined cloud models with both vision and reasoning capabilities. </summary>
public record GenericCloudVisionReasoningModel(
    string Id,
    BackendType Backend,
    string? Name = null,
    uint MaxContextWindowSize = 128000,
    string? Description = null,
    string? SystemMessage = null,
    string? AdditionalPrompt = null
) : CloudModel(Id, Backend, Name, MaxContextWindowSize, Description, SystemMessage), IVisionModel, IReasoningModel
{   
    // IVisionModel - null for cloud (handled by provider API)
    public string? MMProjectPath => null;
    
    // IReasoningModel - null for cloud (handled by provider API)
    public Func<string, Models.ThinkingState, Models.LLMTokenValue>? ReasonFunction => null;
    public string? AdditionalPrompt { get; } = AdditionalPrompt;
}

/// <summary> Generic class for runtime defined local models. </summary>
public record GenericLocalModel(
    string FileName,
    string? Name = null,
    string? Id = null,
    Uri? DownloadUrl = null,
    uint MaxContextWindowSize = 4096,
    string? CustomPath = null,
    string? Description = null,
    string? SystemMessage = null
) : LocalModel(Id ?? FileName, FileName, DownloadUrl, Name ?? FileName, MaxContextWindowSize, Description, SystemMessage)
{   
    /// <summary> Custom path override for the model file (only for dynamically loaded models). </summary>
    public string? CustomPath { get; set; } = CustomPath;
    
    public override bool IsDownloaded(string basePath)
        => File.Exists(Path.Combine(CustomPath ?? basePath, FileName));
        
    public override string GetFullPath(string basePath)
        => Path.Combine(CustomPath ?? basePath, FileName);
}

/// <summary> Generic class for runtime defined local models with reasoning capability. </summary>
public record GenericReasoningModel(
    string FileName,
    Func<string, Models.ThinkingState, Models.LLMTokenValue> ReasonFunction,
    string? Name = null,
    string? Id = null,
    Uri? DownloadUrl = null,
    uint MaxContextWindowSize = 4096,
    string? CustomPath = null,
    string? AdditionalPrompt = null,
    string? Description = null,
    string? SystemMessage = null
) : LocalModel(Id ?? FileName, FileName, DownloadUrl, Name ?? FileName, MaxContextWindowSize, Description, SystemMessage), IReasoningModel
{
    public string? CustomPath { get; set; } = CustomPath;
    
    // IReasoningModel implementation
    public Func<string, Models.ThinkingState, Models.LLMTokenValue> ReasonFunction { get; } = ReasonFunction;
    public string? AdditionalPrompt { get; } = AdditionalPrompt;
    
    public override bool IsDownloaded(string basePath)
        => File.Exists(Path.Combine(CustomPath ?? basePath, FileName));
        
    public override string GetFullPath(string basePath)
        => Path.Combine(CustomPath ?? basePath, FileName);
}

/// <summary> Generic class for runtime defined local models with vision capability. </summary>
public record GenericVisionModel(
    string FileName,
    string MMProjectPath,
    string? Name = null,
    string? Id = null,
    Uri? DownloadUrl = null,
    uint MaxContextWindowSize = 4096,
    string? CustomPath = null,
    string? Description = null,
    string? SystemMessage = null
) : LocalModel(Id ?? FileName, FileName, DownloadUrl, Name ?? FileName, MaxContextWindowSize, Description, SystemMessage), IVisionModel
{
    public string? CustomPath { get; set; } = CustomPath;
    
    // IVisionModel implementation
    public string MMProjectPath { get; } = MMProjectPath;
    
    public override bool IsDownloaded(string basePath)
        => File.Exists(Path.Combine(CustomPath ?? basePath, FileName));
        
    public override string GetFullPath(string basePath)
        => Path.Combine(CustomPath ?? basePath, FileName);
}

/// <summary> Generic class for runtime defined local models with both vision and reasoning capabilities. </summary>
public record GenericVisionReasoningModel(
    string FileName,
    string MMProjectPath,
    Func<string, Models.ThinkingState, Models.LLMTokenValue> ReasonFunction,
    string? Name = null,
    string? Id = null,
    Uri? DownloadUrl = null,
    uint MaxContextWindowSize = 4096,
    string? CustomPath = null,
    string? AdditionalPrompt = null,
    string? Description = null,
    string? SystemMessage = null
) : LocalModel(Id ?? FileName, FileName, DownloadUrl, Name ?? FileName, MaxContextWindowSize, Description, SystemMessage), IVisionModel, IReasoningModel
{
    public string? CustomPath { get; set; } = CustomPath;
    
    // IVisionModel implementation
    public string MMProjectPath { get; } = MMProjectPath;
    
    // IReasoningModel implementation
    public Func<string, ThinkingState, LLMTokenValue> ReasonFunction { get; } = ReasonFunction;
    public string? AdditionalPrompt { get; } = AdditionalPrompt;
    
    public override bool IsDownloaded(string basePath)
        => File.Exists(Path.Combine(CustomPath ?? basePath, FileName));
        
    public override string GetFullPath(string basePath)
        => Path.Combine(CustomPath ?? basePath, FileName);
}
