using System.Text.Json.Serialization;

namespace AzStorageExplorer.Models;

public class SavedQuery
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("database")]
    public string Database { get; set; } = string.Empty;
    
    [JsonPropertyName("container")]
    public string Container { get; set; } = string.Empty;
    
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

