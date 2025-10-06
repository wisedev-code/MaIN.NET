using SixLabors.ImageSharp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<StableDiffusionService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors();

app.MapPost("/generate", async (GenerateRequest request, StableDiffusionService sdService) =>
{
    if (string.IsNullOrWhiteSpace(request?.Prompt))
    {
        return Results.BadRequest("Prompt cannot be empty.");
    }

    Image image;

    try
    {
        image = await Task.Run(() => sdService.GenerateImage(request.Prompt));
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error generating image: {ex.Message}");
    }

    var memoryStream = new MemoryStream();
    await image.SaveAsPngAsync(memoryStream);
    memoryStream.Position = 0;

    var safeFileName = new string(request.Prompt.Take(20).Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
    return Results.File(memoryStream, "image/png", $"image-for-{safeFileName}.png");
});

app.Run();

public record GenerateRequest(string? Prompt);
