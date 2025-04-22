using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Entities;

MaINBootstrapper.Initialize();

// await AIHub.Chat()
//     .WithModel("gemma2:2b")
//     .WithMessage("Hello, World!")
//     .CompleteAsync(interactive: true);

var result = AIHub.Chat()
    .WithModel("llama3.1:8b")
    .WithMessage("Output this invoice as JSON")
    .WithMemoryParams(new MemoryParams()
    {
        AnswerTokens = 1024,
        ContextSize = 4096
    })
    .WithFiles(["3.pdf"], preProcess: true);
    
var chatResult = await result.CompleteAsync();
Console.WriteLine(chatResult.Message.Content);

