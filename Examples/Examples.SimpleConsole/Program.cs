using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Entities;

MaINBootstrapper.Initialize();

// await AIHub.Chat()
//     .WithModel("gemma2:2b")
//     .WithMessage("Hello, World!")
//     .CompleteAsync(interactive: true);

var result = AIHub.Chat()
    .WithModel("gemma3:4b")
    .WithMessage(
        "What is due amount on that invoice?")
    .WithMemoryParams(new MemoryParams() { AnswerTokens = 2137, ContextSize = 1024 })
    .WithInferenceParams(new InferenceParams()
    {
        ContextSize = 1024
    })
    .WithFiles(["./3.pdf"]);
    
var chatResult = await result.CompleteAsync();
Console.WriteLine(chatResult.Message.Content);



