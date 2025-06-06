using System.Text.Json.Serialization;
using MaIN.Domain.Entities;
using MaIN.Services.Dtos;

namespace MaIN.Services.Services.Models;

public class McpResult
{
    public required string Model { get; init; }
    public DateTime CreatedAt { get; set; }
    public required Message Message { get; init; } 
}