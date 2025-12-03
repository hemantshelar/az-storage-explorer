using System.Text.Json.Serialization;

namespace AzStorageExplorer.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StorageType
{
    CosmosDb,
    Blob,
    Table,
    Queue
}

