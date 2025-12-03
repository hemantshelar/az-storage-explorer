using System.Text.Json.Serialization;

namespace AzStorageExplorer.Models;

public class TableStorageConfiguration
{
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;
    
    [JsonPropertyName("savedQueries")]
    public List<SavedQuery> SavedQueries { get; set; } = new();
}

