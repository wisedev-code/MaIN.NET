using Examples;
using Examples.Agents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MaIN.Core;
using MaIN.Services.Steps;

var services = new ServiceCollection();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();
services.AddSingleton<IConfiguration>(configuration);

services.AddMaIN(configuration);
//services.AddTransient<IExample, ChatExample>();
//services.AddTransient<IExample, ChatWithFilesExample>();
//services.AddTransient<IExample, ChatWithVisionExample>();
//services.AddTransient<IExample, ChatWithImageGenExample>();
//services.AddTransient<IExample, ChatFromExistingExample>();
//services.AddTransient<IExample, AgentExample>();
services.AddTransient<IExample, AgentWithRedirectExample>();


var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseMaIN();

//Required for AgentContext examples
serviceProvider.UseMaINAgentFramework();
    
var example = serviceProvider.GetRequiredService<IExample>();
await example.Start();

