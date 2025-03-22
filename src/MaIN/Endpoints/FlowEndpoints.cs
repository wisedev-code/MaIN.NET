using System.Text.Json;
using MaIN.Services.Dtos.Rag;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace MaIN.Endpoints;

public static class FlowEndpoints
{
    public static WebApplication MapFlowEndpoints(this WebApplication app)
    {
        var flowGroup = app.MapGroup("/api/flows")
            .WithTags("Flows");

        flowGroup.MapGet("/{id}", async (HttpContext context,
                [FromServices] IAgentFlowService agentFlowService, string id) =>
            {
                var flow = await agentFlowService.GetFlowById(id);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(flow.ToDto()));
            })
            .WithName("GetFlowById")
            .WithDescription("Get a flow by ID");

        flowGroup.MapGet("/", async (HttpContext context,
                [FromServices] IAgentFlowService agentFlowService) =>
            {
                var flows = await agentFlowService.GetAllFlows();
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(flows.Select(x => x.ToDto())));
            })
            .WithName("GetAllFlows")
            .WithDescription("Get all available flows");

        flowGroup.MapPost("/", async (HttpContext context,
                [FromServices] IAgentFlowService agentFlowService, AgentFlowDto request) =>
            {
                var flow = await agentFlowService.CreateFlow(request.ToDomain());
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(flow.ToDto()));
            })
            .WithName("CreateFlow")
            .WithDescription("Create a new flow");

        flowGroup.MapDelete("/{id}", async (HttpContext context,
                [FromServices] IAgentFlowService agentFlowService, string id) =>
            {
                await agentFlowService.DeleteFlow(id);
                context.Response.ContentType = "application/json";
                return Results.NoContent();
            })
            .WithName("DeleteFlow")
            .WithDescription("Delete a flow by ID");

        return app;
    }
}