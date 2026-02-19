using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

namespace Examples.Chat;

public class ChatWithFilesExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        List<string> files = [
            Path.Combine(AppContext.BaseDirectory, "Files", "Nicolaus_Copernicus.pdf"),
            Path.Combine(AppContext.BaseDirectory, "Files", "Galileo_Galilei.pdf"),
        ];
        
        var result = await AIHub.Chat()
            .WithModel<Gemma3_4b>()
            .WithMessage("You have 2 documents in memory. Whats the difference of work between Galileo and Copernicus?. Give answer based on the documents.")
            .WithFiles(files)
            .DisableCache()
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
        Console.ReadKey();
    }
}