namespace AzStorageExplorer.Services;

public interface ITableStorageService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<List<string>> GetTablesAsync(string connectionString);
    Task<List<Dictionary<string, object>>> QueryEntitiesAsync(
        string connectionString, 
        string tableName, 
        string? filter = null,
        int maxItems = 100);
    Task<Dictionary<string, object>?> GetEntityAsync(
        string connectionString, 
        string tableName, 
        string partitionKey, 
        string rowKey);
}

