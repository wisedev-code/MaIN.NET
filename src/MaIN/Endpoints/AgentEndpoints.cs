using System.Text.Json;
using MaIN.Services.Dtos;
using MaIN.Services.Dtos.Rag;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace MaIN.Endpoints;

public static class AgentEndpoints
{
    public static WebApplication MapAgentEndpoints(this WebApplication app)
    {
        var agentGroup = app.MapGroup("/api/agents")
            .WithTags("Agents");

        agentGroup.MapPost("/{agentId}/process", async (HttpContext context,
                [FromServices] IAgentService agentService,
                string agentId,
                ChatDto request) =>
            {
                var chat = await agentService.Process(request.ToDomain(), agentId);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(chat?.ToDto()));
            })
            .WithName("ProcessAgent")
            .WithDescription("Process a request with a specific agent");

        agentGroup.MapPost("/", async (HttpContext context,
                [FromServices] IAgentService agentService,
                AgentDto request) =>
            {
                var agentExists = await agentService.AgentExists(request.Id);
                if (agentExists) return Results.NoContent();
                var agent = await agentService.CreateAgent(request.ToDomain());
                var chat = await agentService.GetChatByAgent(agent.Id);
                context.Response.ContentType = "application/json";
                return Results.Ok(chat.ToDto());
            })
            .WithName("CreateAgent")
            .WithDescription("Create a new agent");

        agentGroup.MapGet("/", async (HttpContext context,
                [FromServices] IAgentService agentService) =>
            {
                var agents = await agentService.GetAgents();
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(agents.Select(x => x.ToDto())));
            })
            .WithName("GetAllAgents")
            .WithDescription("Get all available agents");

        agentGroup.MapGet("/{id}", async (HttpContext context,
                [FromServices] IAgentService agentService, string id) =>
            {
                var agent = await agentService.GetAgentById(id);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(agent!.ToDto()));
            })
            .WithName("GetAgentById")
            .WithDescription("Get an agent by ID");

        agentGroup.MapGet("/{id}/chat", async (HttpContext context,
                [FromServices] IAgentService agentService, string id) =>
            {
                var chat = await agentService.GetChatByAgent(id);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(chat.ToDto()));
            })
            .WithName("GetChatByAgentId")
            .WithDescription("Get chat associated with an agent");

        agentGroup.MapPut("/{id}/chat/reset", async ([FromServices] IAgentService agentService, string id) =>
            {
                await agentService.Restart(id);
                return Results.Ok();
            })
            .WithName("ResetAgentChat")
            .WithDescription("Reset an agent's chat");

        agentGroup.MapDelete("/{id}", async ([FromServices] IAgentService agentService,
                string id) =>
            {
                await agentService.DeleteAgent(id);
                return Results.NoContent();
            })
            .WithName("DeleteAgent")
            .WithDescription("Delete an agent by ID");

        return app;
    }
    
    public static WebApplication MapInitialAgents(this WebApplication app)
    {
        app.MapPost("/api/agents/init", async (HttpContext context,
                [FromServices] ILLMService llmService,
                [FromServices] IAgentService agentService) =>
            {
                var agents = JsonSerializer.Deserialize<List<AgentDto>>(
                    await File.ReadAllTextAsync("./initial_agents.json"), 
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                
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
            })
            .WithName("InitializeAgents")
            .WithDescription("Initialize agents from configuration file");
            
        return app;
    }
}