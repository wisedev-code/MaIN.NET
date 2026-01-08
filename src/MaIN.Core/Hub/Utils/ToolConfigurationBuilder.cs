using System.Text.Json;
using MaIN.Domain.Entities.Tools;

namespace MaIN.Core.Hub.Utils;
//TODO try to share logic of adding tool to the list across methods https://github.com/wisedev-code/MaIN.NET/pull/98#discussion_r2454997846
public sealed class ToolsConfigurationBuilder
{
    private readonly ToolsConfiguration _config = new() { Tools = [] };
    
    public ToolsConfigurationBuilder AddDefaultTool(
        string type)
    {
        _config.Tools.Add(new ToolDefinition
        {
            Type = type
        });
        return this;
    }
    
    public ToolsConfigurationBuilder AddTool(
        string name, 
        string description, 
        object parameters,
        Func<string, Task<string>> execute)
    {
        _config.Tools.Add(new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = name,
                Description = description,
                Parameters = parameters
            },
            Execute = execute
        });
        return this;
    }

    public ToolsConfigurationBuilder AddTool(
        string name, 
        string description, 
        object parameters,
        Func<string, string> execute)
    {
        _config.Tools!.Add(new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = name,
                Description = description,
                Parameters = parameters
            },
            Execute = args => Task.FromResult(execute(args))
        });
        return this;
    }

    public ToolsConfigurationBuilder AddTool<TArgs>(
        string name, 
        string description, 
        object parameters,
        Func<TArgs, Task<object>> execute) where TArgs : class
    {
        _config.Tools.Add(new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = name,
                Description = description,
                Parameters = parameters
            },
            Execute = async (argsJson) =>
            {
                var args = JsonSerializer.Deserialize<TArgs>(argsJson, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                var result = await execute(args);
                return JsonSerializer.Serialize(result);
            }
        });
        return this;
    }

    public ToolsConfigurationBuilder AddTool<TArgs>(
        string name, 
        string description, 
        object parameters,
        Func<TArgs, object> execute) where TArgs : class
    {
        _config.Tools!.Add(new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = name,
                Description = description,
                Parameters = parameters
            },
            Execute = (argsJson) =>
            {
                var args = JsonSerializer.Deserialize<TArgs>(argsJson, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                var result = execute(args);
                return Task.FromResult(JsonSerializer.Serialize(result));
            }
        });
        return this;
    }

    public ToolsConfigurationBuilder AddTool(
        string name, 
        string description,
        Func<Task<object>> execute)
    {
        _config.Tools.Add(new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = name,
                Description = description,
                Parameters = new { type = "object", properties = new { } }
            },
            Execute = async (args) =>
            {
                var result = await execute();
                return JsonSerializer.Serialize(result);
            }
        });
        return this;
    }

    public ToolsConfigurationBuilder AddTool(
        string name, 
        string description,
        Func<object> execute)
    {
        _config.Tools.Add(new ToolDefinition
        {
            Function = new FunctionDefinition
            {
                Name = name,
                Description = description,
                Parameters = new { type = "object", properties = new { } }
            },
            Execute = (args) =>
            {
                var result = execute();
                return Task.FromResult(JsonSerializer.Serialize(result));
            }
        });
        return this;
    }

    public ToolsConfigurationBuilder WithToolChoice(string choice)
    {
        _config.ToolChoice = choice;
        return this;
    }

    public ToolsConfiguration Build() => _config;
}