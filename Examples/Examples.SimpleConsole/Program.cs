using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Models.Concrete;

MaINBootstrapper.Initialize();

var modelContext = AIHub.Model();

// Get models using ModelRegistry
var gemma = modelContext.GetModel("gemma3-4b");
var llama = modelContext.GetModel("llama3.2-3b");

// Or use strongly-typed models directly
var gemma2b = new Gemma2_2b();
Console.WriteLine($"Model: {gemma2b.Name}, File: {gemma2b.FileName}");

// Download a model
await modelContext.DownloadAsync(gemma2b.Id);
