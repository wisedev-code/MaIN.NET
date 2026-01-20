using MaIN.Domain.Models;

namespace MaIN.Domain.Models.Abstract;

/// <summary>
/// Interface for models that support vision/image input capabilities.
/// </summary>
public interface IVisionModel
{
    /// <summary>
    /// Path to multimodal projector file required for vision processing.
    /// Null for cloud models (handled by provider API).
    /// </summary>
    string? MMProjectPath { get; }
}

/// <summary>
/// Interface for models that support reasoning/thinking capabilities.
/// </summary>
public interface IReasoningModel
{
    /// <summary>
    /// Function to process reasoning tokens.
    /// Null for cloud models (reasoning handled by provider API).
    /// </summary>
    Func<string, ThinkingState, LLMTokenValue>? ReasonFunction { get; }
    
    /// <summary>
    /// Additional prompt added to enable reasoning mode.
    /// </summary>
    string? AdditionalPrompt { get; }
}

// TODO: use it with existing embedding model
/// <summary>
/// Interface for models that support embeddings generation.
/// </summary>
public interface IEmbeddingModel
{
    /// <summary>
    /// Dimension of the embedding vector.
    /// </summary>
    int EmbeddingDimension { get; }
}

// TODO: use it with existing TTS model
/// <summary>
/// Interface for models that support text-to-speech.
/// </summary>
public interface ITTSModel
{
    /// <summary>
    /// Available voices for the TTS model.
    /// </summary>
    IReadOnlyList<string> AvailableVoices { get; }
}
