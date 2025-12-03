namespace AzStorageExplorer.Services;

public interface IQueueStorageService
{
    Task<bool> TestConnectionAsync(string connectionString);
    Task<List<string>> GetQueuesAsync(string connectionString);
    Task<List<QueueMessage>> PeekMessagesAsync(string connectionString, string queueName, int maxMessages = 32);
    Task<QueueProperties> GetQueuePropertiesAsync(string connectionString, string queueName);
}

public class QueueMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTimeOffset? InsertedOn { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
    public long DequeueCount { get; set; }
}

public class QueueProperties
{
    public string Name { get; set; } = string.Empty;
    public int ApproximateMessagesCount { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

