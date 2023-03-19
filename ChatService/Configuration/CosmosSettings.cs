namespace ChatService.Configuration;

public record CosmosSettings
{
    public string ConnectionString { get; init; }
}