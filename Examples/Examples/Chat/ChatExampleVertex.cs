using Examples.Utils;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Models;

namespace Examples.Chat;

public class ChatExampleVertex : IExample
{
    public async Task Start()
    {
        VertexExample.Setup(); //We need to provide Google service account config 
        Console.WriteLine("(Vertex AI) ChatExample is running!");

        await AIHub.Chat()
            .WithModel(Models.Vertex.Gemini2_5Pro)
            .WithMessage("Is the killer whale the smartest animal?")
            .WithInferenceParams(new VertexInferenceParams
            {
                Location = "europe-central2"
            })
            .CompleteAsync(interactive: true);
    }
}
