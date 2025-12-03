using System.Text.Json.Serialization;

namespace AzStorageExplorer.Models;

public class ProjectConfiguration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public List<StorageType> Types { get; set; } = new();
    
    [JsonPropertyName("cosmosDb")]
    public CosmosDbConfiguration? CosmosDb { get; set; }
    
    [JsonPropertyName("blobStorage")]
    public BlobStorageConfiguration? BlobStorage { get; set; }
    
    [JsonPropertyName("tableStorage")]
    public TableStorageConfiguration? TableStorage { get; set; }
    
    [JsonPropertyName("queueStorage")]
    public QueueStorageConfiguration? QueueStorage { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

