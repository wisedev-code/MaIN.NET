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

public partial class Program {}