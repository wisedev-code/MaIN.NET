namespace Examples.Utils;

public static class Tools
{
    public static object GetCurrentTime()
    {
        var now = DateTime.Now;
        var timeInfo = new
        {
            date = now.ToString("yyyy-MM-dd"),
            time = now.ToString("HH:mm:ss"),
            dayOfWeek = now.DayOfWeek.ToString()
        };
        
        return $"Date: {timeInfo.date}, Time: {timeInfo.time}, Day of week: {timeInfo.dayOfWeek}";
    }
}

public static class NoteTools
{
    private const string NotesFolder = "notes";
    
    public static async Task<object> ListNotes(ListNotesArgs args)
    {
        if (!Directory.Exists(args.Folder))
            Directory.CreateDirectory(args.Folder);

        var files = Directory.GetFiles(args.Folder, "*.txt");
        
        return new
        {
            count = files.Length,
            notes = files.Select(f => new
            {
                name = Path.GetFileNameWithoutExtension(f),
                lastModified = File.GetLastWriteTime(f).ToString("yyyy-MM-dd HH:mm:ss"),
                sizeBytes = new FileInfo(f).Length
            }).ToArray()
        };
    }

    public static async Task<object> ReadNote(ReadNoteArgs args)
    {
        var filePath = Path.Combine(NotesFolder, $"{args.NoteName}.txt");
        
        if (!File.Exists(filePath))
            return new { error = $"Note '{args.NoteName}' not found" };

        var content = await File.ReadAllTextAsync(filePath);
        
        return new
        {
            name = args.NoteName,
            content = content,
            lastModified = File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public static async Task<object> SaveNote(SaveNoteArgs args)
    {
        if (!Directory.Exists(NotesFolder))
            Directory.CreateDirectory(NotesFolder);

        var filePath = Path.Combine(NotesFolder, $"{args.NoteName}.txt");
        var isNew = !File.Exists(filePath);
        
        await File.WriteAllTextAsync(filePath, args.Content);
        
        return new
        {
            success = true,
            name = args.NoteName,
            action = isNew ? "created" : "updated",
            path = filePath
        };
    }
}

public class ListNotesArgs
{
    public string Folder { get; set; } = "notes";
}

public class ReadNoteArgs
{
    public string NoteName { get; set; } = null!;
}

public class SaveNoteArgs
{
    public string NoteName { get; set; } = null!;
    public string Content { get; set; } = null!;
}