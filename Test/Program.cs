using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Contexts;
using MaIN.Services.Services.Models;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        MaINBootstrapper.Initialize();

        ChatContext chat = AIHub.Chat();
        chat.WithModel("gemma2:2bc");
        chat.WithMessage("Where do hedgehogs goes at night?");
        await chat.CompleteAsync(interactive: true);

        chat.WithMessage("What were you talking about in the previous message?.");
        await chat.CompleteAsync(interactive: true);
    }
}