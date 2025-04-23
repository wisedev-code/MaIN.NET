using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Entities;

MaINBootstrapper.Initialize();

await AIHub.Chat()
    .WithModel("gemma2:2b")
    .WithMessage("Hello, World!")
    .CompleteAsync(interactive: true);



