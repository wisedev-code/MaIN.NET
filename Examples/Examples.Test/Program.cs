using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Examples.Test;
using MaIN.Core;

var services = new ServiceCollection();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();
services.AddSingleton<IConfiguration>(configuration);

services.AddMaIN(configuration);
services.AddTransient<IExample, Example>();

// Build the service provider
var serviceProvider = services.BuildServiceProvider();

// Resolve and run the service using ActivatorUtilities
var example = serviceProvider.GetRequiredService<IExample>();
await example.Start();

// Example service interface and implementation