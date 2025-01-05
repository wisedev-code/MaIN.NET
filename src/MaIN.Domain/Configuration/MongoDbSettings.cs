namespace MaIN.Services.Configuration;

public class MongoDbSettings
{
    public string? ConnectionString { get; set; }
    public string? DatabaseName { get; set; }
    public string? ChatsCollection { get; set; }
    public string? AgentsCollection { get; set; }
    public string? FlowsCollection { get; set; }
}