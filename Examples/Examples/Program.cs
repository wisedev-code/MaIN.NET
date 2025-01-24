using Examples;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MaIN.Core;

var services = new ServiceCollection();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();
services.AddSingleton<IConfiguration>(configuration);

services.AddMaIN(configuration);
services.AddTransient<IExample, ChatExample>();

var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseMaIN();    
    
var example = serviceProvider.GetRequiredService<IExample>();
await example.Start();

