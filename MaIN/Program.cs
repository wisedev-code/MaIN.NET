using System.Text.Json;
using MaIN.Infrastructure;
using MaIN.Services;
using MaIN.Services.Mappers;
using MaIN.Services.Models;
using MaIN.Services.Services.Abstract;
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
            policy.AllowAnyOrigin() // React app origin
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

app.MapGet("/api/chats", async ([FromServices] IChatService chatService)
    => Results.Ok((await chatService.GetAll()).Select(x => x.ToDto())));


app.Run();