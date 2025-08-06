namespace MaIN.Domain.Entities.Agents.AgentSource;

public class AgentApiSourceDetails : AgentSourceDetailsBase, IAgentSource
{
    public required string Url { get; set; }
    public required string Method { get; init; }
    public string? Payload { get; set; }
    public string? Query { get; set; }
    public string? ResponseType { get; init; }
    public int? ChunkLimit { get; init; }
    public AuthTypeEnum? AuthorisationType { get; set; }
    public string? AuthorisationToken { get; set; }
    /// <summary>
    /// Override payload and authorisation token
    /// </summary>
    public string? Curl { get; set; }
    /// <summary>
    /// Only use with BasicAuth
    /// </summary>
    public string? UserName { get; set; }
    /// <summary>
    /// Only use with BasicAuth
    /// </summary>
    public string? UserPassword { get; set; }
}


public enum AuthTypeEnum
    {
        Bearer = 0,
        ApiKey = 1,
        Basic = 2,

    }