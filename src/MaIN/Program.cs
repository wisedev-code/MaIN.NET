using System.Text.Json;
using MaIN.Domain.Entities;
using MaIN.Domain.Entities.Agents;
using MaIN.Infrastructure;
using MaIN.Models.Rag;
using MaIN.Services;
using MaIN.Services.Mappers;
using MaIN.Services.Models;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Steps;
using Microsoft.AspNetCore.Http.HttpResults;
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
app.Services.InitializeAgents();

//load initial agents configuration
var agentService = app.Services.GetRequiredService<IAgentService>();
var agents = JsonSerializer.Deserialize<List<AgentDto>>(File.ReadAllText("./initial_agents.json"), new JsonSerializerOptions()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

foreach (var agent in agents!)
{
    var existingAgent = await agentService.GetAgentById(agent.Id);
    if(existingAgent != null) continue;
    await agentService.CreateAgent(agent.ToDomain());
}


app.MapPost("/api/agents/{agentId}/process", async (HttpContext context,
    [FromServices] IAgentService agentService,
    string agentId,
    ChatDto request) =>
{
    var chat = await agentService.Process(request.ToDomain(), agentId);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(chat?.ToDto()));
});

app.MapPost("/api/agents", async (HttpContext context,
    [FromServices] IAgentService agentService,
    AgentDto request) =>
{
    var agentExists = await agentService.AgentExists(request.Id);
    if(agentExists) return Results.NoContent();
    var agent = await agentService.CreateAgent(request.ToDomain());
    var chat = await agentService.GetChatByAgent(agent.Id);
    context.Response.ContentType = "application/json";
    return Results.Ok(chat.ToDto());
});

app.MapGet("/api/agents", async (HttpContext context, 
    [FromServices] IAgentService agentService) =>
{
    var agents = await agentService.GetAgents();
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(agents.Select(x => x.ToDto())));
});

app.MapGet("/api/agents/{id}/chat", async (HttpContext context,
    [FromServices] IAgentService agentService, string id) =>
{
    var chat = await agentService.GetChatByAgent(id);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(chat.ToDto()));
});

app.MapPut("/api/agents/{id}/chat/reset", async ([FromServices] IAgentService agentService, string id) =>
{
    await agentService.Restart(id);
    return Results.Ok();
});

app.MapGet("/api/agents/{id}", async (HttpContext context,
    [FromServices] IAgentService agentService, string id) =>
{
    var agent = await agentService.GetAgentById(id);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(agent.ToDto()));
});

app.MapDelete("/api/agents/{id}", async ([FromServices] IAgentService agentService,
    string id) =>
{
    await agentService.DeleteAgent(id);
    return Results.NoContent();
});

app.MapPost("/api/chats/complete", async (HttpContext context,
    [FromServices] IChatService chatService,
    ChatDto request,
    [FromQuery] bool translate = false) =>
{
    var chat = await chatService.Completions(request.ToDomain(), translate);
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
    => Results.Ok((await chatService.GetAll()).Where(x => x.Type == ChatType.Conversation).Select(x => x.ToDto())));

app.Run();