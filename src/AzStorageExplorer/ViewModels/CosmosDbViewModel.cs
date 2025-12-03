using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using AzStorageExplorer.Models;
using AzStorageExplorer.Services;

namespace AzStorageExplorer.ViewModels;

public partial class CosmosDbViewModel : ObservableObject
{
    private readonly ICosmosDbService _cosmosDbService;
    private CosmosDbConfiguration? _configuration;
    private string _connectionString = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _databases = new();

    [ObservableProperty]
    private string? _selectedDatabase;

    [ObservableProperty]
    private ObservableCollection<string> _containers = new();

    [ObservableProperty]
    private string? _selectedContainer;

    [ObservableProperty]
    private string _queryText = "SELECT * FROM c";

    [ObservableProperty]
    private string _queryResults = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SavedQuery> _savedQueries = new();

    [ObservableProperty]
    private SavedQuery? _selectedQuery;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private double _lastRequestCharge;

    [ObservableProperty]
    private int _resultCount;

    private string? _continuationToken;

    public CosmosDbViewModel()
    {
        _cosmosDbService = App.Services.GetRequiredService<ICosmosDbService>();
    }

    public void Initialize(CosmosDbConfiguration configuration, string connectionString)
    {
        _configuration = configuration;
        _connectionString = connectionString;
        
        SavedQueries.Clear();
        foreach (var query in configuration.SavedQueries)
        {
            SavedQueries.Add(query);
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            StatusMessage = "Connection string is required";
            return;
        }

        IsLoading = true;
        StatusMessage = "Connecting...";

        try
        {
            var success = await _cosmosDbService.TestConnectionAsync(_connectionString);
            if (success)
            {
                IsConnected = true;
                StatusMessage = "Connected successfully. Loading databases...";
                await LoadDatabasesAsync();
            }
            else
            {
                StatusMessage = "Connection failed - check connection string";
            }
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"Connection error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadDatabasesAsync()
    {
        if (!IsConnected) return;

        IsLoading = true;
        StatusMessage = "Loading databases...";
        try
        {
            var databases = await _cosmosDbService.GetDatabasesAsync(_connectionString);
            // Assign new collection to trigger PropertyChanged
            Databases = new ObservableCollection<string>(databases);
            
            if (Databases.Any())
            {
                SelectedDatabase = Databases.First();
                StatusMessage = $"Found {Databases.Count} database(s)";
            }
            else
            {
                StatusMessage = "No databases found in this account";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading databases: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedDatabaseChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadContainersAsync();
            
            if (_configuration != null)
            {
                _configuration.DatabaseId = value;
            }
        }
    }

    [RelayCommand]
    private async Task LoadContainersAsync()
    {
        if (!IsConnected || string.IsNullOrEmpty(SelectedDatabase)) return;

        IsLoading = true;
        try
        {
            var containers = await _cosmosDbService.GetContainersAsync(_connectionString, SelectedDatabase);
            // Assign new collection to trigger PropertyChanged
            Containers = new ObservableCollection<string>(containers);
            
            if (Containers.Any())
            {
                SelectedContainer = Containers.First();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading containers: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExecuteQueryAsync()
    {
        if (!IsConnected || string.IsNullOrEmpty(SelectedDatabase) || string.IsNullOrEmpty(SelectedContainer))
        {
            StatusMessage = "Please select a database and container first";
            return;
        }

        if (string.IsNullOrWhiteSpace(QueryText))
        {
            StatusMessage = "Query text is required";
            return;
        }

        IsLoading = true;
        StatusMessage = "Executing query...";
        _continuationToken = null;

        try
        {
            var queryResult = await _cosmosDbService.ExecuteQueryAsync(
                _connectionString,
                SelectedDatabase,
                SelectedContainer,
                QueryText);

            _continuationToken = queryResult.ContinuationToken;
            LastRequestCharge = queryResult.RequestCharge;
            ResultCount = queryResult.Results.Count;

            // Use Newtonsoft.Json to serialize Cosmos results so they look
            // like the emulator (including all properties and values).
            QueryResults = JsonConvert.SerializeObject(queryResult.Results, Formatting.Indented);
            StatusMessage = $"Query completed. {ResultCount} results. RU charge: {LastRequestCharge:F2}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Query error: {ex.Message}";
            QueryResults = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadMoreResultsAsync()
    {
        if (string.IsNullOrEmpty(_continuationToken)) return;

        IsLoading = true;
        try
        {
            var queryResult = await _cosmosDbService.ExecuteQueryAsync(
                _connectionString,
                SelectedDatabase!,
                SelectedContainer!,
                QueryText,
                continuationToken: _continuationToken);

            _continuationToken = queryResult.ContinuationToken;
            LastRequestCharge += queryResult.RequestCharge;
            ResultCount += queryResult.Results.Count;

            // Append to existing results, serialized with Newtonsoft.Json
            var newResults = JsonConvert.SerializeObject(queryResult.Results, Formatting.Indented);
            QueryResults += "\n" + newResults;
            StatusMessage = $"Loaded more results. Total: {ResultCount}. Total RU: {LastRequestCharge:F2}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading more: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SaveQuery(string queryName)
    {
        if (string.IsNullOrWhiteSpace(queryName) || string.IsNullOrWhiteSpace(QueryText))
            return;

        var query = new SavedQuery
        {
            Name = queryName,
            Container = SelectedContainer ?? string.Empty,
            Query = QueryText
        };

        SavedQueries.Add(query);
        _configuration?.SavedQueries.Add(query);
        StatusMessage = $"Query saved: {queryName}";
    }

    [RelayCommand]
    private void LoadQuery(SavedQuery query)
    {
        QueryText = query.Query;
        if (!string.IsNullOrEmpty(query.Container) && Containers.Contains(query.Container))
        {
            SelectedContainer = query.Container;
        }
        SelectedQuery = query;
    }

    [RelayCommand]
    private void DeleteQuery(SavedQuery query)
    {
        SavedQueries.Remove(query);
        _configuration?.SavedQueries.Remove(query);
        StatusMessage = $"Query deleted: {query.Name}";
    }

    [RelayCommand]
    private void UpdateQuery()
    {
        if (SelectedQuery == null) return;

        SelectedQuery.Query = QueryText;
        SelectedQuery.Container = SelectedContainer ?? string.Empty;
        SelectedQuery.LastModified = DateTime.UtcNow;
        StatusMessage = $"Query updated: {SelectedQuery.Name}";
    }
}

