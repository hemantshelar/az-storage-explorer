using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzStorageExplorer.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly Dictionary<string, BlobServiceClient> _clientCache = new();

    private BlobServiceClient GetOrCreateClient(string connectionString)
    {
        if (!_clientCache.TryGetValue(connectionString, out var client))
        {
            client = new BlobServiceClient(connectionString);
            _clientCache[connectionString] = client;
        }
        return client;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            var client = GetOrCreateClient(connectionString);
            await foreach (var _ in client.GetBlobContainersAsync().AsPages(pageSizeHint: 1))
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

    public async Task<List<string>> GetContainersAsync(string connectionString)
    {
        var containers = new List<string>();
        var client = GetOrCreateClient(connectionString);
        
        await foreach (var container in client.GetBlobContainersAsync())
        {
            containers.Add(container.Name);
        }
        
        return containers;
    }

    public async Task<List<BlobItem>> GetBlobsAsync(string connectionString, string containerName, string? prefix = null)
    {
        var blobs = new List<BlobItem>();
        var client = GetOrCreateClient(connectionString);
        var containerClient = client.GetBlobContainerClient(containerName);
        
        await foreach (var item in containerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/"))
        {
            if (item.IsPrefix)
            {
                blobs.Add(new BlobItem
                {
                    Name = item.Prefix.TrimEnd('/'),
                    IsDirectory = true
                });
            }
            else if (item.IsBlob)
            {
                blobs.Add(new BlobItem
                {
                    Name = item.Blob.Name,
                    IsDirectory = false,
                    Size = item.Blob.Properties.ContentLength,
                    LastModified = item.Blob.Properties.LastModified,
                    ContentType = item.Blob.Properties.ContentType
                });
            }
        }
        
        return blobs;
    }

    public async Task<byte[]> DownloadBlobAsync(string connectionString, string containerName, string blobName)
    {
        var client = GetOrCreateClient(connectionString);
        var containerClient = client.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var response = await blobClient.DownloadContentAsync();
        return response.Value.Content.ToArray();
    }

    public async Task<string> GetBlobContentAsStringAsync(string connectionString, string containerName, string blobName)
    {
        var bytes = await DownloadBlobAsync(connectionString, containerName, blobName);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    public async Task<BlobProperties> GetBlobPropertiesAsync(string connectionString, string containerName, string blobName)
    {
        var client = GetOrCreateClient(connectionString);
        var containerClient = client.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        
        var props = await blobClient.GetPropertiesAsync();
        
        return new BlobProperties
        {
            Name = blobName,
            Size = props.Value.ContentLength,
            ContentType = props.Value.ContentType,
            LastModified = props.Value.LastModified,
            CreatedOn = props.Value.CreatedOn,
            ETag = props.Value.ETag.ToString(),
            Metadata = props.Value.Metadata
        };
    }
}

