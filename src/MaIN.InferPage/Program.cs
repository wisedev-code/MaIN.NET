using MaIN.Core;
using MaIN.Domain.Configuration;
using MaIN.Domain.Models;
using Microsoft.FluentUI.AspNetCore.Components;
using MaIN.InferPage.Components;
using MaIN.Services.Services.LLMService.Utils;
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

    if (!string.IsNullOrEmpty(modelArg))
    {
        Utils.Model = modelArg;

        if (string.IsNullOrEmpty(modelPathArg))
        {
            Console.WriteLine("Error: A model path must be provided using --path when a model is specified.");
            return;
        }
        Utils.Path = modelPathArg;

        var envModelsPath = Environment.GetEnvironmentVariable("MaIN_ModelsPath");
        if (string.IsNullOrEmpty(envModelsPath))
        {
            Console.Write("Please enter the MaIN_ModelsPath: ");
            envModelsPath = Console.ReadLine();
            Environment.SetEnvironmentVariable("MaIN_ModelsPath", envModelsPath);
        }
    }
    else
    {
        Console.WriteLine("No model argument provided. Continuing without model configuration.");
    }

    if (backendArg != null)
    {
        var apiKeyVariable = "";
        var apiName = "";

        switch (backendArg.ToLower())
        {
            case "openai":
                Utils.OpenAi = true;
                apiKeyVariable = LLMApiRegistry.OpenAi.ApiKeyEnvName;
                apiName = LLMApiRegistry.OpenAi.ApiName;
                break;

            case "gemini":
                Utils.Gemini = true;
                apiKeyVariable = LLMApiRegistry.Gemini.ApiKeyEnvName;
                apiName = LLMApiRegistry.Gemini.ApiName;
                break;
            
            case "deepseek":
                Utils.DeepSeek = true;
                apiKeyVariable = LLMApiRegistry.Deepseek.ApiKeyEnvName;
                apiName = LLMApiRegistry.Deepseek.ApiName;
                break;

            case "groqcloud":
                Utils.GroqCloud = true;
                apiKeyVariable = LLMApiRegistry.Groq.ApiKeyEnvName;
                apiName = LLMApiRegistry.Groq.ApiName;
                break;

            case "anthropic":
                Utils.Anthropic = true;
                apiKeyVariable = LLMApiRegistry.Anthropic.ApiKeyEnvName;
                apiName = LLMApiRegistry.Anthropic.ApiName;
                break;

            case "xai":
                Utils.Xai = true;
                apiKeyVariable = LLMApiRegistry.Xai.ApiKeyEnvName;
                apiName = LLMApiRegistry.Xai.ApiName;
                break;

            case "ollama":
                Utils.Ollama = true;
                apiKeyVariable = LLMApiRegistry.Ollama.ApiKeyEnvName;
                apiName = LLMApiRegistry.Ollama.ApiName;
                break;
        }

        var key = Environment.GetEnvironmentVariable(apiKeyVariable);
        if (string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(apiName) && !string.IsNullOrEmpty(apiKeyVariable))
        {
            Console.Write($"Please enter your {apiName} API key: ");
            key = Console.ReadLine();
            Environment.SetEnvironmentVariable(apiKeyVariable, key);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error during parameter processing: " + ex.Message);
    return;
}

if (Utils.OpenAi)
{
    builder.Services.AddMaIN(builder.Configuration, settings =>
    {
        settings.BackendType = BackendType.OpenAi;
    });
}
else if (Utils.Gemini)
{
    builder.Services.AddMaIN(builder.Configuration, settings =>
    {
        settings.BackendType = BackendType.Gemini;
    });
}
else if (Utils.DeepSeek)
{
    builder.Services.AddMaIN(builder.Configuration, settings =>
    {
        settings.BackendType = BackendType.DeepSeek;
    });
}
else if (Utils.GroqCloud)
{
    builder.Services.AddMaIN(builder.Configuration, settings =>
    {
        settings.BackendType = BackendType.GroqCloud;
    });
}
else if(Utils.Anthropic)
{
    builder.Services.AddMaIN(builder.Configuration, settings =>
    {
        settings.BackendType = BackendType.Anthropic;
    });
}
else if (Utils.Xai)
{
    builder.Services.AddMaIN(builder.Configuration, settings =>
    {
        settings.BackendType = BackendType.Xai;
    });
}
else if (Utils.Ollama)
{
    builder.Services.AddMaIN(builder.Configuration, settings =>
    {
        settings.BackendType = BackendType.Ollama;
    });
}
else
{
    if (Utils.Path == null && !KnownModels.IsModelSupported(Utils.Model!))
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
