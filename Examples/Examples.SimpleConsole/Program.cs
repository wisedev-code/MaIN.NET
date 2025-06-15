using MaIN.Core;
using MaIN.Core.Hub;
using MaIN.Domain.Entities;
using OpenAI.Models;

MaINBootstrapper.Initialize();

var model = AIHub.Model();

var m = model.GetModel("gemma3:4b");

var x = model.GetModel("llama3.2:3b");
await model.DownloadAsync(x.Name);


