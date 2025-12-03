using System.Text.Json.Serialization;

namespace AzStorageExplorer.Models;

public class BlobStorageConfiguration
{
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;
    
    [JsonPropertyName("lastAccessedContainer")]
    public string? LastAccessedContainer { get; set; }
}

