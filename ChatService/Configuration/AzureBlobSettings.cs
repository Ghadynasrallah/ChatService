namespace ChatService.Configuration;

public record AzureBlobSettings()
{ 
    public string ConnectionString { get; init; }
}
