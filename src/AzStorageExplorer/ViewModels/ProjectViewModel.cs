using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AzStorageExplorer.Models;

namespace AzStorageExplorer.ViewModels;

public partial class ProjectViewModel : ObservableObject
{
    [ObservableProperty]
    private ProjectConfiguration _configuration;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private StorageType _selectedStorageType = StorageType.CosmosDb;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Not connected";

    public ObservableCollection<StorageType> AvailableStorageTypes { get; } = new();

    public ProjectViewModel(ProjectConfiguration configuration, string? filePath)
    {
        _configuration = configuration;
        _filePath = filePath;
        
        UpdateAvailableStorageTypes();
    }

    private void UpdateAvailableStorageTypes()
    {
        AvailableStorageTypes.Clear();
        foreach (var type in Configuration.Types)
        {
            AvailableStorageTypes.Add(type);
        }
        
        if (AvailableStorageTypes.Any())
        {
            SelectedStorageType = AvailableStorageTypes.First();
        }
    }

    [RelayCommand]
    private void ToggleStorageType(StorageType type)
    {
        if (Configuration.Types.Contains(type))
        {
            Configuration.Types.Remove(type);
        }
        else
        {
            Configuration.Types.Add(type);
            
            // Initialize configuration for new type
            switch (type)
            {
                case StorageType.CosmosDb when Configuration.CosmosDb == null:
                    Configuration.CosmosDb = new CosmosDbConfiguration();
                    break;
                case StorageType.Blob when Configuration.BlobStorage == null:
                    Configuration.BlobStorage = new BlobStorageConfiguration();
                    break;
                case StorageType.Table when Configuration.TableStorage == null:
                    Configuration.TableStorage = new TableStorageConfiguration();
                    break;
                case StorageType.Queue when Configuration.QueueStorage == null:
                    Configuration.QueueStorage = new QueueStorageConfiguration();
                    break;
            }
        }
        
        UpdateAvailableStorageTypes();
        IsDirty = true;
    }

    public void UpdateConnectionString(StorageType type, string connectionString)
    {
        switch (type)
        {
            case StorageType.CosmosDb:
                Configuration.CosmosDb ??= new CosmosDbConfiguration();
                Configuration.CosmosDb.ConnectionString = connectionString;
                break;
            case StorageType.Blob:
                Configuration.BlobStorage ??= new BlobStorageConfiguration();
                Configuration.BlobStorage.ConnectionString = connectionString;
                break;
            case StorageType.Table:
                Configuration.TableStorage ??= new TableStorageConfiguration();
                Configuration.TableStorage.ConnectionString = connectionString;
                break;
            case StorageType.Queue:
                Configuration.QueueStorage ??= new QueueStorageConfiguration();
                Configuration.QueueStorage.ConnectionString = connectionString;
                break;
        }
        IsDirty = true;
    }

    public string GetConnectionString(StorageType type)
    {
        return type switch
        {
            StorageType.CosmosDb => Configuration.CosmosDb?.ConnectionString ?? string.Empty,
            StorageType.Blob => Configuration.BlobStorage?.ConnectionString ?? string.Empty,
            StorageType.Table => Configuration.TableStorage?.ConnectionString ?? string.Empty,
            StorageType.Queue => Configuration.QueueStorage?.ConnectionString ?? string.Empty,
            _ => string.Empty
        };
    }
}

