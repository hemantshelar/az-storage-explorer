using Azure.Data.Tables;

namespace AzStorageExplorer.Services;

public class TableStorageService : ITableStorageService
{
    private readonly Dictionary<string, TableServiceClient> _clientCache = new();

    private TableServiceClient GetOrCreateClient(string connectionString)
    {
        if (!_clientCache.TryGetValue(connectionString, out var client))
        {
            client = new TableServiceClient(connectionString);
            _clientCache[connectionString] = client;
        }
        return client;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            var client = GetOrCreateClient(connectionString);
            await foreach (var _ in client.QueryAsync().AsPages(pageSizeHint: 1))
            {
                break;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetTablesAsync(string connectionString)
    {
        var tables = new List<string>();
        var client = GetOrCreateClient(connectionString);
        
        await foreach (var table in client.QueryAsync())
        {
            tables.Add(table.Name);
        }
        
        return tables;
    }

    public async Task<List<Dictionary<string, object>>> QueryEntitiesAsync(
        string connectionString,
        string tableName,
        string? filter = null,
        int maxItems = 100)
    {
        var results = new List<Dictionary<string, object>>();
        var client = GetOrCreateClient(connectionString);
        var tableClient = client.GetTableClient(tableName);
        
        var query = tableClient.QueryAsync<TableEntity>(filter: filter, maxPerPage: maxItems);
        
        await foreach (var entity in query)
        {
            var dict = new Dictionary<string, object>
            {
                ["PartitionKey"] = entity.PartitionKey,
                ["RowKey"] = entity.RowKey,
                ["Timestamp"] = entity.Timestamp ?? DateTimeOffset.MinValue
            };
            
            foreach (var key in entity.Keys)
            {
                if (key != "PartitionKey" && key != "RowKey" && key != "Timestamp" && key != "odata.etag")
                {
                    dict[key] = entity[key];
                }
            }
            
            results.Add(dict);
            
            if (results.Count >= maxItems)
                break;
        }
        
        return results;
    }

    public async Task<Dictionary<string, object>?> GetEntityAsync(
        string connectionString,
        string tableName,
        string partitionKey,
        string rowKey)
    {
        var client = GetOrCreateClient(connectionString);
        var tableClient = client.GetTableClient(tableName);
        
        try
        {
            var response = await tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
            var entity = response.Value;
            
            var dict = new Dictionary<string, object>
            {
                ["PartitionKey"] = entity.PartitionKey,
                ["RowKey"] = entity.RowKey,
                ["Timestamp"] = entity.Timestamp ?? DateTimeOffset.MinValue
            };
            
            foreach (var key in entity.Keys)
            {
                if (key != "PartitionKey" && key != "RowKey" && key != "Timestamp" && key != "odata.etag")
                {
                    dict[key] = entity[key];
                }
            }
            
            return dict;
        }
        catch
        {
            return null;
        }
    }
}

