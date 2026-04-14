using MaIN.Core;
using MaIN.Domain.Configuration;
using MaIN.Domain.Configuration.Vertex;

namespace Examples.Utils;

public class VertexExample
{
    public static void Setup()
    {
        MaINBootstrapper.Initialize(configureSettings: options =>
        {
            options.BackendType = BackendType.Vertex;
            options.GoogleServiceAccountAuth = new GoogleServiceAccountConfig
            {
                ProjectId = "<YOUR_GCP_PROJECT_ID>",
                ClientEmail = "<YOUR_SERVICE_ACCOUNT_EMAIL>",
                PrivateKey = @"<YOUR_PRIVATE_KEY>"
            };
        });
    }
}