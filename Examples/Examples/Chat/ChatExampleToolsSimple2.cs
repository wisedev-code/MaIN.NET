using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;
using MaIN.Domain.Configuration;

namespace Examples.Chat;

public class ChatExampleToolsSimple2 : IExample
{
    public async Task Start()
    {
        //OpenAiExample.Setup(); //We need to provide OpenAi API key
        
        Console.WriteLine("(OpenAi) ChatExample is running!"); 
        
        await AIHub.Chat()
            .WithBackend(BackendType.OpenAi)
            .WithModel("gpt-5-nano")
            .WithMessage("What time is it right now?")
            .WithTools(new ToolsConfigurationBuilder()
                .AddTool<ListNotesArgs>(
                    "list_notes",
                    "List all available notes",
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            folder = new { type = "string", description = "Notes folder", @default = "notes" }
                        }
                    },
                    NoteTools.ListNotes)
                .AddTool<ReadNoteArgs>(
                    "read_note",
                    "Read the content of a specific note",
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            noteName = new { type = "string", description = "Name of the note (without .txt extension)" }
                        },
                        required = new[] { "noteName" }
                    },
                    NoteTools.ReadNote)
                .AddTool<SaveNoteArgs>(
                    "save_note",
                    "Save or update a note with new content",
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            noteName = new { type = "string", description = "Name of the note (without .txt extension)" },
                            content = new { type = "string", description = "Content to save in the note" }
                        },
                        required = new[] { "noteName", "content" }
                    },
                    NoteTools.SaveNote)
                .WithToolChoice("auto")
                .Build())
            .CompleteAsync(interactive: true);
    }
}