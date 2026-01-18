using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Tools;
using MaIN.Domain.Models;
using MaIN.Services.Services.Models;
using FileInfo = MaIN.Domain.Entities.FileInfo;

namespace MaIN.Core.Hub.Contexts.Interfaces.ChatContext;

public interface IChatCompletionBuilder : IChatActions
{
    /// <summary>
    /// Configures low-level LLM parameters like temperature, context size, max tokens etc.
    /// </summary>
    /// <param name="inferenceParams">An object containing detailed inference settings for the LLM.</param>
    IChatCompletionBuilder WithInferenceParams(InferenceParams inferenceParams);
    
    /// <summary>
    /// Attaches external tools/functions that the model can invoke during the conversation.
    /// </summary>
    /// <param name="toolsConfiguration">Configuration defining available tools and their execution modes.</param>
    IChatCompletionBuilder WithTools(ToolsConfiguration toolsConfiguration);
    
    /// <summary>
    /// Defines memory parameters for the chat.
    /// </summary>
    /// <param name="memoryParams">Configuration for how the chat context and memory should be handled.</param>
    IChatCompletionBuilder WithMemoryParams(MemoryParams memoryParams);
    
    /// <summary>
    /// Configures the session to use Text-to-Speech for the model's responses.
    /// </summary>
    /// <param name="speechParams">Parameters for the voice synthesis.</param>
    IChatCompletionBuilder Speak(TextToSpeechParams speechParams);
    
    /// <summary>
    /// Sets the specific execution backend for the inference (e.g., Local, Cloud).
    /// </summary>
    /// <param name="backendType">The type of backend to be used for processing the request.</param>
    IChatCompletionBuilder WithBackend(BackendType backendType);
    
    /// <summary>
    /// Sets a system-level prompt to guide the model's behavior, persona, and constraints.
    /// </summary>
    /// <param name="systemPrompt">The text content of the system instructions.</param>
    IChatCompletionBuilder WithSystemPrompt(string systemPrompt);
    
    /// <summary>
    /// Attaches a list of files provided as <see cref="FileStream"/> to the message context.
    /// </summary>
    /// <param name="file">A list of open file streams to be uploaded or analyzed.</param>
    /// <param name="preProcess">If true, the files will be pre-processed (e.g., indexed) before sending.</param>
    IChatCompletionBuilder WithFiles(List<FileStream> file, bool preProcess = false);
    
    /// <summary>
    /// Attaches a list of files provided as <see cref="FileInfo"/> objects to the message context.
    /// </summary>
    /// <param name="file">A list of file metadata and content references.</param>
    /// <param name="preProcess">If true, the files will be pre-processed (e.g., indexed) before sending.</param>
    IChatCompletionBuilder WithFiles(List<FileInfo> file, bool preProcess = false);
    
    /// <summary>
    /// Attaches a list of files from provided local system paths to the message context.
    /// </summary>
    /// <param name="file">A list of absolute or relative paths to the files.</param>
    /// <param name="preProcess">If true, the files will be pre-processed (e.g., indexed) before sending.</param>
    IChatCompletionBuilder WithFiles(List<string> file, bool preProcess = false);
    
    /// <summary>
    /// Disables the internal caching mechanism for the upcoming request.
    /// </summary>
    IChatCompletionBuilder DisableCache();
    
    /// <summary>
    /// Sends the configured chat context to the service for completion.
    /// </summary>
    /// <param name="translate">Indicates whether the response should be automatically translated.</param>
    /// <param name="interactive">If true, the response will be processed in streaming mode.</param>
    /// <param name="changeOfValue">An optional callback invoked whenever a new token or update is received during streaming.</param>
    /// <returns>A task representing the asynchronous operation, containing the <see cref="ChatResult"/>.</returns>
    Task<ChatResult> CompleteAsync(bool translate = false, bool interactive = false, Func<LLMTokenValue?, Task>? changeOfValue = null);

}