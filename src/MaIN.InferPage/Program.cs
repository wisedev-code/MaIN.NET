using MaIN.Core;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models.Concrete;
using MaIN.Domain.Models.Abstract;
using Microsoft.FluentUI.AspNetCore.Components;
using MaIN.InferPage.Components;
using Utils = MaIN.InferPage.Utils;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

try
{
    var modelArg = builder.Configuration["model"];
    var modelPathArg = builder.Configuration["path"];
    var backendArg = builder.Configuration["backend"];


    if (backendArg != null)
    {
        Utils.BackendType = backendArg.ToLower() switch
        {
            "openai" => BackendType.OpenAi,
            "gemini" => BackendType.Gemini,
            "deepseek" => BackendType.DeepSeek,
            "groqcloud" => BackendType.GroqCloud,
            "anthropic" => BackendType.Anthropic,
            "xai" => BackendType.Xai,
            "ollama" => BackendType.Ollama,
            _ => BackendType.Self
        };

        if (Utils.BackendType != BackendType.Self)
        {
            var apiKeyVariable = LLMApiRegistry.GetEntry(Utils.BackendType)?.ApiKeyEnvName ?? string.Empty;
            var key = Environment.GetEnvironmentVariable(apiKeyVariable);

            if (string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(apiKeyVariable))
            {
                Console.Write($"Please enter your {Utils.BackendType.ToString()} API key: ");
                key = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(key))
                {
                    Utils.HasApiKey = true;
                    Environment.SetEnvironmentVariable(apiKeyVariable, key);
                }
            }
        }
    }

    if (!string.IsNullOrEmpty(modelArg))
    {
        Utils.Model = modelArg;
        Utils.Path = modelPathArg;

        if (Utils.BackendType == BackendType.Self)
        {
            if (string.IsNullOrEmpty(modelPathArg))
            {
                Console.WriteLine("Error: A model path must be provided using --path when a local model is specified.");
                return;
            }

            var envModelsPath = Environment.GetEnvironmentVariable("MaIN_ModelsPath");
            if (string.IsNullOrEmpty(envModelsPath))
            {
                Console.Write("Please enter the MaIN_ModelsPath: ");
                envModelsPath = Console.ReadLine();
                Environment.SetEnvironmentVariable("MaIN_ModelsPath", envModelsPath);
            }
        }
    }
    else
    {
        Console.WriteLine("No model argument provided. Continuing without model configuration.");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error during parameter processing: " + ex.Message);
    return;
}

if (Utils.BackendType != BackendType.Self)
{
    builder.Services.AddMaIN(builder.Configuration, settings =>
    {
        settings.BackendType = Utils.BackendType;
    });
}
else
{
    if (Utils.Path == null && !ModelRegistry.Exists(Utils.Model!))
    {
        Console.WriteLine($"Model: {Utils.Model} is not supported");
        Environment.Exit(0);
    }

    builder.Services.AddMaIN(builder.Configuration);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.Services.UseMaIN();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
