using Microsoft.Azure.Cosmos;
using System.Text.Json;
using System.Net.Http;
using AzStorageExplorer.Models;

namespace AzStorageExplorer.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly Dictionary<string, CosmosClient> _clientCache = new();

    /// <summary>
    /// Parse a standard Cosmos DB connection string into endpoint + key.
    /// Works with both emulator and real Azure connection strings.
    /// </summary>
    private static (string Endpoint, string Key) ParseConnectionString(string connectionString)
    {
        string? endpoint = null;
        string? key = null;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;

            var name = kv[0].ToLowerInvariant();
            var value = kv[1];

            if (name is "accountendpoint" or "endpoint")
            {
                endpoint = value;
            }
            else if (name is "accountkey" or "key")
            {
                key = value;
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Invalid Cosmos DB connection string. Expected 'AccountEndpoint' and 'AccountKey' values.");
        }

        return (endpoint, key);
    }

    private CosmosClient GetOrCreateClient(string connectionString)
    {
        if (_clientCache.TryGetValue(connectionString, out var existing))
        {
            return existing;
        }

        var (endpoint, key) = ParseConnectionString(connectionString);

        // Detect emulator by endpoint host / well-known key
        var isEmulator =
            endpoint.Contains("localhost:8081", StringComparison.OrdinalIgnoreCase) ||
            endpoint.Contains("127.0.0.1:8081", StringComparison.OrdinalIgnoreCase) ||
            key.StartsWith("C2y6yDjf5", StringComparison.Ordinal);

        CosmosClient client;

        if (isEmulator)
        {
            // Emulator: HTTPS with self-signed certificate. Create a fresh HttpClient
            // per CosmosClient that bypasses certificate validation. This avoids
            // reusing a disposed HttpClient across clients.
            var options = new CosmosClientOptions
            {
                ApplicationName = "AzStorageExplorer",
                ConnectionMode = ConnectionMode.Gateway,
                LimitToEndpoint = true,
                HttpClientFactory = () =>
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    return new HttpClient(handler);
                }
            };

            client = new CosmosClient(endpoint, key, options);
        }
        else
        {
            var options = new CosmosClientOptions
            {
                ApplicationName = "AzStorageExplorer",
                ConnectionMode = ConnectionMode.Gateway
            };

            client = new CosmosClient(endpoint, key, options);
        }

        _clientCache[connectionString] = client;
        return client;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            // Clear cached client to ensure fresh connection with updated settings
            if (_clientCache.TryGetValue(connectionString, out var existing))
            {
                existing.Dispose();
                _clientCache.Remove(connectionString);
            }

            var client = GetOrCreateClient(connectionString);
            var account = await client.ReadAccountAsync();
            return account != null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connection error: {ex}");
            throw; // Re-throw so the ViewModel can capture the actual error
        }
    }

    public async Task<List<string>> GetDatabasesAsync(string connectionString)
    {
        var databases = new List<string>();
        var client = GetOrCreateClient(connectionString);
        
        using var iterator = client.GetDatabaseQueryIterator<DatabaseProperties>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            databases.AddRange(response.Select(db => db.Id));
        }
        
        return databases;
    }

    public async Task<List<string>> GetContainersAsync(string connectionString, string databaseId)
    {
        var containers = new List<string>();
        var client = GetOrCreateClient(connectionString);
        var database = client.GetDatabase(databaseId);
        
        using var iterator = database.GetContainerQueryIterator<ContainerProperties>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            containers.AddRange(response.Select(c => c.Id));
        }
        
        return containers;
    }

    public async Task<CosmosQueryResult> ExecuteQueryAsync(
        string connectionString,
        string databaseId,
        string containerId,
        string query,
        int maxItems = 100,
        string? continuationToken = null)
    {
        var client = GetOrCreateClient(connectionString);
        var container = client.GetContainer(databaseId, containerId);
        
        var queryDefinition = new QueryDefinition(query);
        var requestOptions = new QueryRequestOptions
        {
            MaxItemCount = maxItems
        };

        var result = new CosmosQueryResult();

        using var iterator = container.GetItemQueryIterator<dynamic>(
            queryDefinition, 
            continuationToken, 
            requestOptions);
        
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            result.RequestCharge = response.RequestCharge;
            result.ContinuationToken = response.ContinuationToken;
            
            foreach (var item in response)
            {
                result.Results.Add(item);
            }
        }

        return result;
    }

    public void Dispose()
    {
        foreach (var client in _clientCache.Values)
        {
            client.Dispose();
        }
        _clientCache.Clear();
    }
}

