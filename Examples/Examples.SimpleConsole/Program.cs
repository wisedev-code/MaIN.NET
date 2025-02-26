using MaIN.Core;
using MaIN.Core.Hub;

MaINBootstrapper.Initialize();

await AIHub.Chat()
        .WithModel("gemma2:2b")
        .WithMessage("Hello, World!")
        .CompleteAsync(interactive: true);
        