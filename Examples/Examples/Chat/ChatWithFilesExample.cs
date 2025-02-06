using MaIN.Core.Hub;

namespace Examples;

public class ChatWithFilesExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"];
        
        var result = await AIHub.Chat()
            .WithModel("gemma2:2b")
            .WithMessage("You have 2 documents in memory. Whats the difference of work between Galileo and Copernicus?. Give answer based on the documents.")
            .WithFiles(files)
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
    }
}