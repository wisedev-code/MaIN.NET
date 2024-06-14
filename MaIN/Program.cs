using System.Text.Json;
using System.Text.Json.Serialization;
using GTranslate.Translators;
using MaIN;
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
    [FromServices] IHttpClientFactory httpClientFactory,
    ChatDto request) =>
{
    using var translator = new AggregateTranslator();
    var lng = await translator.DetectLanguageAsync(request.Messages.Last().Content);
    var originalMessages = request.Messages;
    var translatedMessages = request.Messages.Select(m => new Message
    {
        Role = m.Role,
        Content = translator.TranslateAsync(m.Content, "en").Result.Translation
    }).ToList();
    request.Messages = translatedMessages;

    using var client = httpClientFactory.CreateClient();
    var response = await client.PostAsync("http://localhost:11434/api/chat",
        new StringContent(JsonSerializer.Serialize(new ChatRequest
        {
            Messages = request.Messages,
            Model = request.Model,
            Stream = request.Stream
        }), System.Text.Encoding.UTF8, "application/json"));

    if (!response.IsSuccessStatusCode)
    {
        context.Response.StatusCode = (int)response.StatusCode;
        return;
    }

    // Read the response from OpenAI
    var responseBody = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<ChatResponse>(responseBody);
    result!.Message.Content = (await translator.TranslateAsync(result.Message.Content, lng.Name, "en"))
        .Translation;

    originalMessages.Add(result.Message);
    TempDb.Chats[request.Id] = request;
    // Return the response to the client
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(result));
});

app.MapPost("/api/chats", async (HttpContext context,
    [FromServices] IHttpClientFactory httpClientFactory,
    ChatDto request) =>
{
    request.Id = Guid.NewGuid().ToString();
    TempDb.Chats.Add(request.Id, request);
    return Results.Created(request.Id, request);
});

app.MapGet("/api/chats/{id}", async (HttpContext context,
    [FromServices] IHttpClientFactory httpClientFactory, string id) => Results.Ok(TempDb.Chats[id]));

app.MapGet("/api/chats", async (IHttpClientFactory httpClientFactory) => Results.Ok(TempDb.Chats.Values));

app.MapPost("/api/generate", async (HttpContext context,
    [FromServices] IHttpClientFactory httpClientFactory,
    GenerateRequest request) =>
{
    using var translator = new AggregateTranslator();
    var lng = await translator.DetectLanguageAsync(request.Prompt);
    request.Prompt = translator.TranslateAsync(request.Prompt, "en").Result.Translation;

    using var client = httpClientFactory.CreateClient();
    var response = await client.PostAsync("http://localhost:11434/api/generate",
        new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json"));

    if (!response.IsSuccessStatusCode)
    {
        context.Response.StatusCode = (int)response.StatusCode;
        return;
    }

    // Read the response from OpenAI
    var responseBody = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<GenerateResposne>(responseBody);
    result!.Response = (await translator.TranslateAsync(result.Response, lng.Name, "en"))
        .Translation;

    // Return the response to the client
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync(JsonSerializer.Serialize(result));
});

app.Run();


public class Message
{
    [JsonPropertyName("role")] public string Role { get; set; }

    [JsonPropertyName("content")] public string Content { get; set; }
}


public class GenerateRequest
{
    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("prompt")] public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
}

public class GenerateResposne
{
    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("created_at")] public string CreatedAt { get; set; }

    [JsonPropertyName("response")] public string Response { get; set; }

    [JsonPropertyName("done")] public bool Done { get; set; }
}

public class ChatRequest
{
    [JsonPropertyName("model")] public string Model { get; set; }
    [JsonPropertyName("messages")] public List<Message> Messages { get; set; }

    [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
}

public class ChatResponse
{
    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

    [JsonPropertyName("message")] public Message Message { get; set; }

    [JsonPropertyName("done")] public bool Done { get; set; }
}

public class ChatDto
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("messages")] public List<Message> Messages { get; set; }

    [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
}