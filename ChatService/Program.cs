using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using ChatService.Configuration;
using ChatService.Storage;
using Azure.Storage.Blobs;
using ChatService.Services;
using Microsoft.Extensions.Logging.ApplicationInsights;

var builder = WebApplication.CreateBuilder(args);

//Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Configuration
builder.Services.Configure<CosmosSettings>(builder.Configuration.GetSection("Cosmos"));
builder.Services.Configure<AzureBlobSettings>(builder.Configuration.GetSection("AzureBlob"));
builder.Services.Configure<ApplicationInsightSettings>(builder.Configuration.GetSection("ApplicationInsights"));

// The following line enables Application Insights telemetry collection.
var appInsightsSettings = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<ApplicationInsightSettings>>().Value;
builder.Services.AddApplicationInsightsTelemetry(appInsightsSettings.ConnectionString);

//Configure logging
builder.Logging.AddApplicationInsights(
    configureTelemetryConfiguration: (config) => 
        config.ConnectionString = appInsightsSettings.ConnectionString,
    configureApplicationInsightsLoggerOptions: (options) => { }
);

builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Trace);


// Add Services
builder.Services.AddSingleton<IProfileStorage, CosmosProfileStorage>();
builder.Services.AddSingleton<IProfilePictureStorage, CloudBlobProfilePictureStorage>();
builder.Services.AddSingleton<IProfileService, ProfileService>();
builder.Services.AddSingleton<IImageService, ImageService>();
builder.Services.AddSingleton<IConversationService, ConversationService>();
builder.Services.AddSingleton<IConversationStorage, CosmosConversationStorage>();
builder.Services.AddSingleton<IMessageStorage, CosmosMessageStorage>();

builder.Services.AddSingleton(sp =>
{
    var cosmosOptions = sp.GetRequiredService<IOptions<CosmosSettings>>();
    return new CosmosClient(cosmosOptions.Value.ConnectionString);
});

builder.Services.AddSingleton(sp =>
{
    var blobOptions = sp.GetRequiredService<IOptions<AzureBlobSettings>>();
    return new BlobServiceClient(blobOptions.Value.ConnectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program {}