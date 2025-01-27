using MaIN.Core.Hub;

namespace Examples;

public class ChatWithFilesExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"];
        var context = AIHub.Chat().WithModel("phi3:mini").WithFiles(files);
        
        var result = await context
            .WithMessage("You have 2 documents in memory. What is the difference between them?")
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
    }
}