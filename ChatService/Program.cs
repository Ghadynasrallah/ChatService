using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using ChatService.Configuration;
using ChatService.Storage;
using Azure.Storage.Blobs;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

var builder = WebApplication.CreateBuilder(args);

//Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Configuration
builder.Services.Configure<CosmosSettings>(builder.Configuration.GetSection("Cosmos"));
builder.Services.Configure<AzureBlobSettings>(builder.Configuration.GetSection("AzureBlob"));

// Add Services
builder.Services.AddSingleton<IProfileStorage, CosmosProfileStorage>();
builder.Services.AddSingleton<IProfilePictureStorage, CloudBlobProfilePictureStorage>();
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