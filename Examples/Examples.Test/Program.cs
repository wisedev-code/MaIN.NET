using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Examples.Test;
using MaIN.Core;
using MaIN.Core.Interfaces;

var services = new ServiceCollection();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();
services.AddSingleton<IConfiguration>(configuration);

services.AddMaIN(configuration);
services.AddTransient<IExample, Example>();

var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseMaIN();    
    
var example = serviceProvider.GetRequiredService<IExample>();
await example.Start();

