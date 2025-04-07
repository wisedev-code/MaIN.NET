using System.Text.Json;
using MaIN.Domain.Configuration;
using MaIN.Domain.Entities;
using MaIN.Services.Dtos;
using MaIN.Services.Mappers;
using MaIN.Services.Services.Abstract;
using MaIN.Services.Services.ImageGenServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace MaIN.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var chatGroup = app.MapGroup("/api/chats")
            .WithTags("Chats");

        chatGroup.MapPost("/complete", async (
                [FromServices] IChatService chatService,
                ChatDto request,
                [FromQuery] bool translate = false,
                [FromQuery] bool interactiveUpdates = true) =>
            {
                var chat = await chatService.Completions(request.ToDomain(), translate, interactiveUpdates, null);
                return Results.Ok(chat);
            })
            .WithName("CompleteChat")
            .WithDescription("Complete a chat with the LLM service");

        chatGroup.MapPost("/", async (
                [FromServices] IChatService chatService,
                ChatDto request) =>
            {
                request.Id = Guid.NewGuid().ToString();
                await chatService.Create(request.ToDomain());
                return Results.Created(request.Id, request);
            })
            .WithName("CreateChat")
            .WithDescription("Create a new chat");

        chatGroup.MapDelete("/{id}", async (
                [FromServices] IChatService chatService, string id) =>
            {
                await chatService.Delete(id);
                return Results.NoContent();
            })
            .WithName("DeleteChat")
            .WithDescription("Delete a chat by ID");

        chatGroup.MapGet("/{id}", async (
                    [FromServices] IChatService chatService, string id) =>
                Results.Ok((await chatService.GetById(id)).ToDto()))
            .WithName("GetChatById")
            .WithDescription("Get a chat by ID");

        chatGroup.MapGet("/models", async (
                [FromServices] ILLMService llmService,
                [FromServices] IHttpClientFactory httpClientFactory,
                [FromServices] MaINSettings options) =>
            {
                var models = await llmService.GetCurrentModels();
                return models;
            })
            .WithName("GetAvailableModels")
            .WithDescription("Get all available LLM models");

        chatGroup.MapGet("/", async ([FromServices] IChatService chatService) =>
            Results.Ok((await chatService.GetAll()).Where(x => x.Type == ChatType.Conversation).Select(x => x.ToDto())))
            .WithName("GetAllChats")
            .WithDescription("Get all conversation chats");
    }
}