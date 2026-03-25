using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.BackendInferenceParams;
using MaIN.Domain.Configuration.Vertex;
using MaIN.Domain.Models;

namespace Examples.Chat;

public class ChatExampleVertex : IExample
{
    public async Task Start()
    {
        MaINBootstrapper.Initialize(configureSettings: options =>
        {
            options.BackendType = BackendType.Vertex;
            options.GoogleServiceAccountAuth = new GoogleServiceAccountAuth
            {
                ProjectId   = "<YOUR_GCP_PROJECT_ID>",
                ClientEmail = "<YOUR_SERVICE_ACCOUNT_EMAIL>",
                PrivateKey  = "<YOUR_PRIVATE_KEY>"
            };
        });

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
