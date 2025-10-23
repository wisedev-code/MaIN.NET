﻿namespace MaIN.Domain.Entities.Tools;

public class ToolDefinition
{
    public string Type { get; set; } = "function";
    public FunctionDefinition? Function { get; set; } 
    public Func<string, Task<string>>? Execute { get; set; }
}