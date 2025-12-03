using System.Text.Json;
using AzStorageExplorer.Models;

namespace AzStorageExplorer.Services;

public class ProjectService : IProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<ProjectConfiguration> LoadProjectAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Project file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        var config = JsonSerializer.Deserialize<ProjectConfiguration>(json, JsonOptions);
        
        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize project configuration");
        }

        return config;
    }

    public async Task SaveProjectAsync(string filePath, ProjectConfiguration configuration)
    {
        configuration.LastModified = DateTime.UtcNow;
        
        var json = JsonSerializer.Serialize(configuration, JsonOptions);
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await File.WriteAllTextAsync(filePath, json);
    }

    public Task<ProjectConfiguration> CreateNewProjectAsync(string name, List<StorageType> types)
    {
        var config = new ProjectConfiguration
        {
            Name = name,
            Types = types,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        // Initialize storage configurations based on selected types
        if (types.Contains(StorageType.CosmosDb))
        {
            config.CosmosDb = new CosmosDbConfiguration();
        }

        if (types.Contains(StorageType.Blob))
        {
            config.BlobStorage = new BlobStorageConfiguration();
        }

        if (types.Contains(StorageType.Table))
        {
            config.TableStorage = new TableStorageConfiguration();
        }

        if (types.Contains(StorageType.Queue))
        {
            config.QueueStorage = new QueueStorageConfiguration();
        }

        return Task.FromResult(config);
    }

    public bool ValidateConfiguration(ProjectConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Name))
        {
            return false;
        }

        if (configuration.Types.Count == 0)
        {
            return false;
        }

        // Validate that required configurations exist for selected types
        foreach (var type in configuration.Types)
        {
            switch (type)
            {
                case StorageType.CosmosDb when configuration.CosmosDb == null:
                case StorageType.Blob when configuration.BlobStorage == null:
                case StorageType.Table when configuration.TableStorage == null:
                case StorageType.Queue when configuration.QueueStorage == null:
                    return false;
            }
        }

        return true;
    }
}

