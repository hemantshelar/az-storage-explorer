using System.Text.Json.Serialization;

namespace AzStorageExplorer.Models;

public class QueueStorageConfiguration
{
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;
    
    [JsonPropertyName("lastAccessedQueue")]
    public string? LastAccessedQueue { get; set; }
}

