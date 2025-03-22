using MaIN.Core;
using MaIN.Domain.Configuration;
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
    bool openAiFlag = backendArg != null && backendArg.Equals("openai", StringComparison.OrdinalIgnoreCase);

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

    if (openAiFlag)
    {
        Utils.OpenAi = true;
        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(openAiKey))
        {
            Console.Write("Please enter your OpenAI API key: ");
            openAiKey = Console.ReadLine();
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", openAiKey );
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
else
{
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
