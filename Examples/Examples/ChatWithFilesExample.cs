using MaIN.Core.Hub;

namespace Examples;

public class ChatWithFilesExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"];
        
        var result = await AIHub.Chat()
            .WithModel("llama3.1:8b")
            .WithMessage("You have 2 documents in memory. What is the difference between them?")
            .WithFiles(files)
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
    }
}