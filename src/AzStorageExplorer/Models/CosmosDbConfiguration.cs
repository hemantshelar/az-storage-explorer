using System.Text.Json.Serialization;

namespace AzStorageExplorer.Models;

public class CosmosDbConfiguration
{
    [JsonPropertyName("connectionString")]
    public string ConnectionString { get; set; } = string.Empty;
    
    [JsonPropertyName("databaseId")]
    public string DatabaseId { get; set; } = string.Empty;
    
    [JsonPropertyName("savedQueries")]
    public List<SavedQuery> SavedQueries { get; set; } = new();
}

