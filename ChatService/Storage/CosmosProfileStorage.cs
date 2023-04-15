using System.Net;
using Microsoft.Azure.Cosmos;
using ChatService.Dtos;
using ChatService.Exceptions;
using ChatService.Storage.Entities;



namespace ChatService.Storage;

public class CosmosProfileStorage : IProfileStorage
{
    private readonly CosmosClient _cosmosClient;

    public CosmosProfileStorage(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }

    private Container container => _cosmosClient.GetDatabase("ChatService").GetContainer("profiles");
    
    public async Task UpsertProfile(Profile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Username) ||
            string.IsNullOrWhiteSpace(profile.FirstName) ||
            string.IsNullOrWhiteSpace(profile.LastName))
        {
            throw new ArgumentException($"Invalid profile {profile}", nameof(profile));
        }

        await container.UpsertItemAsync(ToEntity(profile));
    }

    public async Task<Profile?> GetProfile(string username)
    {
        try
        {
            var entity = await container.ReadItemAsync<ProfileEntity>(
                id: username,
                partitionKey: new PartitionKey(username),
                new ItemRequestOptions
                {
                    ConsistencyLevel = ConsistencyLevel.Session
                }
            );
            return ToProfile(entity);
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
                return null;
            throw;
        }
    }

    public async Task DeleteProfile(string username)
    { 
        try
        {
            await container.DeleteItemAsync<Profile>(
                partitionKey: new PartitionKey(username),
                id: username
            );
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            throw;
        }
    }
    
    private static ProfileEntity ToEntity(Profile profile)
    {
        return new ProfileEntity(
            partitionKey: profile.Username,
            id: profile.Username,
            FirstName:profile.FirstName,
            LastName:profile.LastName,
            ProfilePictureId: profile.ProfilePictureId
            );
    }

    private static Profile ToProfile(ProfileEntity profileEntity)
    {
        return new Profile(Username:profileEntity.id, 
                            FirstName:profileEntity.FirstName,
                            LastName: profileEntity.LastName,
                            ProfilePictureId: profileEntity.ProfilePictureId
                            );
    }
}
