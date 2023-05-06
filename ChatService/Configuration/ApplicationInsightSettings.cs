namespace ChatService.Configuration;

public record ApplicationInsightSettings
{
    public string ConnectionString { get; init; }
}