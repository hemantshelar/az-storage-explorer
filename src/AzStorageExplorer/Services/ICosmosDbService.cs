using AzStorageExplorer.Models;

namespace AzStorageExplorer.Services;

public interface ICosmosDbService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<List<string>> GetDatabasesAsync(string connectionString);
    Task<List<string>> GetContainersAsync(string connectionString, string databaseId);
    Task<CosmosQueryResult> ExecuteQueryAsync(
        string connectionString, 
        string databaseId, 
        string containerId, 
        string query,
        int maxItems = 100,
        string? continuationToken = null);
}

