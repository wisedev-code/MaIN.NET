namespace MaIN.Services.Services.LLMService;

public class ChatMemoryOptions
{
    public Dictionary<string, string> TextData { get; set; } = [];
    public Dictionary<string, string> FilesData { get; set; } = [];
    public Dictionary<string, FileStream> StreamData { get; set; } = [];
    public List<string> WebUrls { get; set; } = [];
    public List<string>? Memory { get; set; }  //Is this obsolete? TBD validate how its really used
    public bool PreProcess { get; set; } = false;
}