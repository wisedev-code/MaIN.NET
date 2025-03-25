using MaIN.Endpoints;
using MaIN.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureServices();

var app = builder.Build();

app.ConfigureMiddleware();

app.MapAgentEndpoints();
app.MapFlowEndpoints();
app.MapChatEndpoints();

app.MapInitialAgents();

app.Run();

// Grants Program.cs visibility in Integration Tests project assembly
public partial class Program {}