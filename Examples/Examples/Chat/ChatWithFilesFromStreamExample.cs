using MaIN.Core.Hub;

namespace Examples;

//TODO: this will be moved to test cases. It's same as normal files but I want to test streams
public class ChatWithFilesFromStreamExample : IExample
{
    public async Task Start()
    {
        Console.WriteLine("ChatExample with files is running!");

        List<string> files = ["./Files/Nicolaus_Copernicus.pdf", "./Files/Galileo_Galilei.pdf"];
        
        var fileStreams = new List<FileStream>();
        
        foreach (var path in files)
        {
            if (File.Exists(path))
            {
                // Open file with read access
                FileStream fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                    
                fileStreams.Add(fs);
                Console.WriteLine($"Loaded: {path}");
            }
            else
            {
                Console.WriteLine($"File not found: {path}");
            }
        }
        
        var result = await AIHub.Chat()
            .WithModel("gemma2:2b")
            .WithMessage("You have 2 documents in memory. Whats the difference of work between Galileo and Copernicus?. Give answer based on the documents.")
            .WithFiles(fileStreams)
            .CompleteAsync();
        
        Console.WriteLine(result.Message.Content);
        Console.ReadKey();
    }
}