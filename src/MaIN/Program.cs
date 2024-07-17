using System.Text.Json;
using MaIN.Domain.Entities;
using MaIN.Infrastructure;
using MaIN.Models.Rag;
using MaIN.Services;
using MaIN.Services.Mappers;
using MaIN.Services.Models;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Steps;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE",
        policy =>
        {
            policy.AllowAnyOrigin() 
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.ConfigureApplication();
builder.Services.ConfigureInfrastructure(builder.Configuration);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFE");

//Initialize agents flow
var ollamaService = app.Services.GetRequiredService<IOllamaService>();
Actions.Initialize(ollamaService);


app.MapPost("/api/agents/{agentId}/process", async (HttpContext context,
    [FromServices] IAgentService agentService,
    string agentId,
    ChatDto request) =>
{
    var chat = await agentService.Process(request.ToDomain(), agentId);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(chat));
});

app.MapPost("/api/agents", async (HttpContext context,
    [FromServices] IAgentService agentService,
    AgentDto request) =>
{
    var agent = await agentService.CreateAgent(request.ToDomain());
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(agent));
});

app.MapGet("/api/agents", async (HttpContext context, 
    [FromServices] IAgentService agentService) =>
{
    var agents = await agentService.GetAgents();
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(agents));
});

app.MapGet("/api/agents/{id}", async (HttpContext context,
    [FromServices] IAgentService agentService, string id) =>
{
    var agent = await agentService.GetAgentById(id);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(agent));
});

app.MapPost("/api/chats/complete", async (HttpContext context,
    [FromServices] IChatService chatService,
    ChatDto request) =>
{
    var chat = await chatService.Completions(request.ToDomain());
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(chat));
});

app.MapPost("/api/chats", async (HttpContext context,
    [FromServices] IChatService chatService,
    ChatDto request) =>
{
    request.Id = Guid.NewGuid().ToString();
    await chatService.Create(request.ToDomain());
    return Results.Created(request.Id, request);
});

app.MapDelete("/api/chats/{id}", async (HttpContext context,
    [FromServices] IChatService chatService, string id) =>
{
    await chatService.Delete(id);
    return Results.NoContent();
});

app.MapGet("/api/chats/{id}", async (HttpContext context,
    [FromServices] IChatService chatService, string id) => 
    Results.Ok((await chatService.GetById(id)).ToDto()));

app.MapGet("/api/chats/models", async (HttpContext context,
        [FromServices] IOllamaService ollamaService) => 
    Results.Ok((await ollamaService.GetCurrentModels())));

app.MapGet("/api/chats", async ([FromServices] IChatService chatService)
    => Results.Ok((await chatService.GetAll()).Select(x => x.ToDto())));


app.Run();