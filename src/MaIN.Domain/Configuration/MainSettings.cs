namespace MaIN.Domain.Configuration;

public class MainSettings
{
    public MongoDbSettings? MongoDbSettings { get; set; }
    public string? OllamaUrl { get; set; }
    public string? ImageGenUrl { get; set; }
}

public class MongoDbSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public string ChatsCollection { get; set; }
    public string AgentsCollection { get; set; }
    public string FlowsCollection { get; set; }
}