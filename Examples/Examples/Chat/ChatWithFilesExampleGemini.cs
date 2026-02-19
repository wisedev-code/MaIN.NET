using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatWithFilesExampleGemini : IExample
{
    public async Task Start()
    {
        Console.WriteLine("(Gemini) ChatExample is running!");
        GeminiExample.Setup(); //We need to provide Gemini API key

        List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"];

        var result = await AIHub.Chat()
            .WithModel<Gemini2_5Flash>()
            .WithMessage("You have 2 documents in memory. Whats the difference of work between Galileo and Copernicus?. Give answer based on the documents.")
            .WithFiles(files)
            .CompleteAsync(interactive: true);

        Console.WriteLine(result.Message.Content);
        Console.ReadKey();
    }
}