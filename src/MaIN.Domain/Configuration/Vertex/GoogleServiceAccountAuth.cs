namespace MaIN.Domain.Configuration.Vertex;

public class GoogleServiceAccountAuth
{
    public required string ProjectId { get; init; }
    public required string ClientEmail { get; init; }
    public required string PrivateKey { get; init; }
    public string TokenUri { get; init; } = "https://oauth2.googleapis.com/token";
}
