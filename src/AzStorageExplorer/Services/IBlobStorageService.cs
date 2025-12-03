namespace AzStorageExplorer.Services;

public interface IBlobStorageService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<List<string>> GetContainersAsync(string connectionString);
    Task<List<BlobItem>> GetBlobsAsync(string connectionString, string containerName, string? prefix = null);
    Task<byte[]> DownloadBlobAsync(string connectionString, string containerName, string blobName);
    Task<string> GetBlobContentAsStringAsync(string connectionString, string containerName, string blobName);
    Task<BlobProperties> GetBlobPropertiesAsync(string connectionString, string containerName, string blobName);
}

public class BlobItem
{
    public string Name { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long? Size { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? ContentType { get; set; }
}

public class BlobProperties
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public string? ETag { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

