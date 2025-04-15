using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents.AgentSource;
using MaIN.Services.Services.Models.Commands;

MaINBootstrapper.Initialize();

await AIHub.Chat()
    .WithModel("gemma2:2b")
    .WithMessage("Hello, World!")
    .CompleteAsync(interactive: true);

