using System.Text.Json;
using MaIN;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services;
using MaIN.Services.Dtos;
using MaIN.Services.Dtos.Rag;
using MaIN.Services.Mappers;
using MaIN.Services.Services;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.ImageGenServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSignalR();
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

builder.Services.ConfigureMaIN(builder.Configuration);
builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowFE");
app.MapHub<NotificationHub>("/diagnostics");

//load initial agents configuration
var agents = JsonSerializer.Deserialize<List<AgentDto>>(File.ReadAllText("./initial_agents.json"), new JsonSerializerOptions()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

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

app.MapPost("/api/agents/init", async (HttpContext context,
    [FromServices] ILLMService llmService,
    [FromServices] IAgentService agentService) =>
{
    foreach (var agent in agents!)
    {
        var existingAgent = await agentService.GetAgentById(agent.Id);
        if(existingAgent != null) continue;
        var models = await llmService.GetCurrentModels();
        if(models.Contains(agent.Model))
        {
            await agentService.CreateAgent(agent.ToDomain());
        }
    }
    return Results.Ok();
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
    await context.Response.WriteAsync(JsonSerializer.Serialize(agent!.ToDto()));
});

app.MapGet("/api/flows/{id}", async (HttpContext context,
    [FromServices] IAgentFlowService agentFlowService, string id) =>
{
    var flow = await agentFlowService.GetFlowById(id);
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(flow.ToDto()));
});

app.MapGet("/api/flows/", async (HttpContext context,
    [FromServices] IAgentFlowService agentFlowService) =>
{
    var flows = await agentFlowService.GetAllFlows();
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(flows.Select(x => x.ToDto())));
});

app.MapPost("/api/flows/", async (HttpContext context,
    [FromServices] IAgentFlowService agentFlowService, AgentFlowDto request) =>
{
    var flow = await agentFlowService.CreateFlow(request.ToDomain());
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(flow.ToDto()));
});

app.MapDelete("/api/flows/{id}", async (HttpContext context,
    [FromServices] IAgentFlowService agentFlowService, string id) =>
{
    await agentFlowService.DeleteFlow(id);
    context.Response.ContentType = "application/json";
    return Results.NoContent();
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
    [FromQuery] bool translate = false,
        [FromQuery] bool interactiveUpdates = true) =>
{
    var chat = await chatService.Completions(request.ToDomain(), translate, interactiveUpdates, null);
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
    [FromServices] ILLMService llmService, 
    [FromServices] IHttpClientFactory httpClientFactory,
    [FromServices] MaINSettings options) =>
{
    var models = await llmService.GetCurrentModels();
    //add flux support
    var client = httpClientFactory.CreateClient();
    try
    {
        var response = await client.GetAsync(options.ImageGenUrl + "/health");
        if (response.IsSuccessStatusCode)
        {
            models.Add(ImageGenService.Models.FLUX);
        }
    }
    catch (Exception)
    {
        Console.WriteLine("No image-gen service running");
    }
    
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(models));
});
    

app.MapGet("/api/chats", async ([FromServices] IChatService chatService)
    => Results.Ok((await chatService.GetAll()).Where(x => x.Type == ChatType.Conversation).Select(x => x.ToDto())));

app.Run();