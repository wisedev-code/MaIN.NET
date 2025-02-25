using MaIN.Domain.Entities;
using MaIN.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.ConfigureMaIN(builder.Configuration);

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Service API (MaIN LLM Server)",
        Version = "v1",
        Description = "API for interacting with Language Models",
    });
});

// Add services to the DI container
var app = builder.Build();

// Minimal API Endpoints
app.MapPost("/api/llm/send", async ([FromServices] ILLMService llmService, ChatRequest request) =>
{
    var result = await llmService.Send(request.Chat, request.InteractiveUpdates, request.NewSession);
    return result != null 
        ? Results.Ok(result) 
        : Results.BadRequest("Failed to process the chat request.");
});

app.MapPost("/api/llm/askmemory", async ([FromServices] ILLMService llmService, AskMemoryRequest request) =>
{
    var result = await llmService.AskMemory(request.Chat, request.TextData, request.FileData, request.Memory);
    return result != null 
        ? Results.Ok(result) 
        : Results.BadRequest("Failed to process the memory request.");
});

app.MapGet("/api/llm/models", async ([FromServices] ILLMService llmService) =>
{
    var models = await llmService.GetCurrentModels();
    return Results.Ok(models);
});

app.MapDelete("/api/llm/session/{chatId}", async ([FromServices] ILLMService llmService, string chatId) =>
{
    await llmService.CleanSessionCache(chatId);
    return Results.Ok($"Session chat {chatId} has been cleared.");
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "LLM Service API v1");
    options.RoutePrefix = "swagger"; 
});
app.Run();

// Request DTOs
record ChatRequest(Chat? Chat, bool InteractiveUpdates = false, bool NewSession = false);

record AskMemoryRequest(Chat? Chat, Dictionary<string,string>? TextData = null, Dictionary<string,string>? FileData = null, List<string>? Memory = null);