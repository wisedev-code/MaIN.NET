 using Examples.SimpleConsole;
 using MaIN.Core;
 using MaIN.Core.Hub;
 using MaIN.Core.Hub.Utils;
 using MaIN.Domain.Configuration;
 using MaIN.NET.Examples.TogetherAI;

 MaINBootstrapper.Initialize();
 Console.WriteLine("Image Agent - FLUX.1 schnell (~$0.028/image)");
        Console.WriteLine("Set TOGETHER_API_KEY and IMGBB_API_KEY environment variables");
        Console.WriteLine();

        var context = await AIHub.Agent()
            .WithBackend(BackendType.GroqCloud)
            .WithModel("meta-llama/llama-4-maverick-17b-128e-instruct")
            .WithSteps(StepBuilder.Instance
                .Answer()
                .Build())
            .WithTools(new ToolsConfigurationBuilder()
                .AddTool<GenerateImageArgs>(
                    "generate_image",
                    "Generate an image from a text prompt. Returns imgbb URL.",
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            prompt = new
                            {
                                type = "string",
                                description = "Description of the image to generate"
                            }
                        },
                        required = new[] { "prompt" }
                    },
                    ImageTools.GenerateImage)
                .AddTool<EditImageArgs>(
                    "edit_image",
                    "Edit an existing image using its URL. When user says 'add X to it' or similar, use the URL from the previous generate/edit response in this conversation.",
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            imageUrl = new
                            {
                                type = "string",
                                description = "URL of the image to edit (from previous generate/edit or external URL)"
                            },
                            prompt = new
                            {
                                type = "string",
                                description = "How to modify the image"
                            }
                        },
                        required = new[] { "imageUrl", "prompt" }
                    },
                    ImageTools.EditImage)
                
                .WithToolChoice("auto")
                .Build())
            .CreateAsync(interactiveResponse: true);

        // Example: Natural conversation flow with URLs
        await context.ProcessAsync("Generate a cat");
        
        Console.WriteLine("\n--//--\n");
        
        // Claude should remember the URL from above and use it here
        await context.ProcessAsync("Nice! Now add a wizard hat to it");
