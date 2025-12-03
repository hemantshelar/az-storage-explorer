using Azure.Storage.Queues;

namespace AzStorageExplorer.Services;

public class QueueStorageService : IQueueStorageService
{
    private readonly Dictionary<string, QueueServiceClient> _clientCache = new();

    private QueueServiceClient GetOrCreateClient(string connectionString)
    {
        if (!_clientCache.TryGetValue(connectionString, out var client))
        {
            client = new QueueServiceClient(connectionString);
            _clientCache[connectionString] = client;
        }
        return client;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            var client = GetOrCreateClient(connectionString);
            await foreach (var _ in client.GetQueuesAsync().AsPages(pageSizeHint: 1))
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

    public async Task<List<string>> GetQueuesAsync(string connectionString)
    {
        var queues = new List<string>();
        var client = GetOrCreateClient(connectionString);
        
        await foreach (var queue in client.GetQueuesAsync())
        {
            queues.Add(queue.Name);
        }
        
        return queues;
    }

    public async Task<List<QueueMessage>> PeekMessagesAsync(string connectionString, string queueName, int maxMessages = 32)
    {
        var messages = new List<QueueMessage>();
        var client = GetOrCreateClient(connectionString);
        var queueClient = client.GetQueueClient(queueName);
        
        var peekedMessages = await queueClient.PeekMessagesAsync(maxMessages);
        
        foreach (var msg in peekedMessages.Value)
        {
            messages.Add(new QueueMessage
            {
                MessageId = msg.MessageId,
                MessageText = msg.MessageText,
                InsertedOn = msg.InsertedOn,
                ExpiresOn = msg.ExpiresOn,
                DequeueCount = msg.DequeueCount
            });
        }
        
        return messages;
    }

    public async Task<QueueProperties> GetQueuePropertiesAsync(string connectionString, string queueName)
    {
        var client = GetOrCreateClient(connectionString);
        var queueClient = client.GetQueueClient(queueName);
        
        var props = await queueClient.GetPropertiesAsync();
        
        return new QueueProperties
        {
            Name = queueName,
            ApproximateMessagesCount = props.Value.ApproximateMessagesCount,
            Metadata = props.Value.Metadata
        };
    }
}

