using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Core.Hub.Utils;

namespace Examples.Agents;

public class AgentExampleTools : IExample
{
    public async Task Start()
    {
        AnthropicExample.Setup();
        Console.WriteLine("(Anthropic) Tool example is running!");

        var context = await AIHub.Agent()
            .WithModel("claude-sonnet-4-5-20250929")
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Build())
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
                            noteName = new
                                { type = "string", description = "Name of the note (without .txt extension)" }
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
                            noteName = new
                                { type = "string", description = "Name of the note (without .txt extension)" },
                            content = new { type = "string", description = "Content to save in the note" }
                        },
                        required = new[] { "noteName", "content" }
                    },
                    NoteTools.SaveNote)
                .WithToolChoice("auto")
                .Build())
            .CreateAsync(interactiveResponse: true);

        await context.ProcessAsync("What notes do I currently have?");
        
        Console.WriteLine("--//--");
        
        await context.ProcessAsync("Create a new note for a shopping list that includes healthy foods.");

    }
}