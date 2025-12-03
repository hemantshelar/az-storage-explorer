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
    private bool _isLoading;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private double _lastRequestCharge;

    [ObservableProperty]
    private int _resultCount;

    [ObservableProperty]
    private ObservableCollection<QueryResultEntry> _queryHistory = new();

    private string? _continuationToken;

    public CosmosDbViewModel()
    {
        _cosmosDbService = App.Services.GetRequiredService<ICosmosDbService>();
    }

    public void Initialize(CosmosDbConfiguration configuration, string connectionString)
    {
        _configuration = configuration;
        _connectionString = connectionString;
        
        // Seed the query history from any saved queries in the configuration
        QueryHistory.Clear();
        if (configuration.SavedQueries != null)
        {
            foreach (var saved in configuration.SavedQueries)
            {
                // Use saved query's database, fall back to config's databaseId if empty
                var database = !string.IsNullOrEmpty(saved.Database) 
                    ? saved.Database 
                    : configuration.DatabaseId;
                    
                QueryHistory.Add(new QueryResultEntry
                {
                    Database = database,
                    Container = saved.Container,
                    QueryText = saved.Query,
                    SavedQuery = saved
                });
            }
        }
    }

    public void UpdateConnectionString(string connectionString)
    {
        _connectionString = connectionString;
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

    private async Task ExecuteQueryCoreAsync(string database, string container, string queryText, QueryResultEntry? targetEntry = null)
    {
        IsLoading = true;
        StatusMessage = "Executing query...";
        _continuationToken = null;

        try
        {
            var queryResult = await _cosmosDbService.ExecuteQueryAsync(
                _connectionString,
                database,
                container,
                queryText);

            _continuationToken = queryResult.ContinuationToken;
            LastRequestCharge = queryResult.RequestCharge;
            ResultCount = queryResult.Results.Count;

            // Serialize results like the emulator
            var json = JsonConvert.SerializeObject(queryResult.Results, Formatting.Indented);
            QueryResults = json;

            // Update existing entry or create a new one
            QueryResultEntry entry;

            if (targetEntry != null)
            {
                entry = targetEntry;
                entry.ResultsJson = json;
                entry.ResultCount = queryResult.Results.Count;
                entry.RequestCharge = queryResult.RequestCharge;
                entry.ExecutedAtUtc = DateTime.UtcNow;
            }
            else
            {
                // Create a SavedQuery so this query is persisted in the configuration
                SavedQuery? saved = null;
                if (_configuration != null)
                {
                    saved = new SavedQuery
                    {
                        Name = $"Query {_configuration.SavedQueries.Count + 1}",
                        Database = database,
                        Container = container,
                        Query = queryText
                    };
                    _configuration.SavedQueries.Add(saved);
                }

                entry = new QueryResultEntry
                {
                    Database = database,
                    Container = container,
                    QueryText = queryText,
                    ResultsJson = json,
                    ResultCount = queryResult.Results.Count,
                    RequestCharge = queryResult.RequestCharge,
                    ExecutedAtUtc = DateTime.UtcNow,
                    SavedQuery = saved
                };

                QueryHistory.Insert(0, entry);
            }

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

        await ExecuteQueryCoreAsync(SelectedDatabase, SelectedContainer, QueryText);
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
    private async Task RunFromHistoryAsync(QueryResultEntry entry)
    {
        if (!IsConnected)
        {
            StatusMessage = "Please connect first.";
            return;
        }

        // Use entry's values if set, otherwise fall back to current selection
        var databaseToUse = !string.IsNullOrEmpty(entry.Database) ? entry.Database : SelectedDatabase;
        var containerToUse = !string.IsNullOrEmpty(entry.Container) ? entry.Container : SelectedContainer;
        var queryToUse = entry.QueryText?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(databaseToUse))
        {
            StatusMessage = "Please select a database first.";
            return;
        }

        if (string.IsNullOrEmpty(containerToUse))
        {
            StatusMessage = "Please enter a container name.";
            return;
        }

        if (string.IsNullOrEmpty(queryToUse))
        {
            StatusMessage = "Query text cannot be empty.";
            return;
        }

        System.Diagnostics.Debug.WriteLine($"Running query: DB={databaseToUse}, Container={containerToUse}, Query={queryToUse}");

        // Update the UI selection if they exist in the lists
        if (Databases.Contains(databaseToUse))
        {
            SelectedDatabase = databaseToUse;
        }
        if (Containers.Contains(containerToUse))
        {
            SelectedContainer = containerToUse;
        }
        QueryText = queryToUse;

        // Update the entry with the values being used
        entry.Database = databaseToUse;
        entry.Container = containerToUse;

        await ExecuteQueryCoreAsync(databaseToUse, containerToUse, queryToUse, entry);
    }

    [RelayCommand]
    private void SaveHistory(QueryResultEntry entry)
    {
        entry.QueryText = entry.QueryText ?? string.Empty;

        // Use currently selected database if entry doesn't have one
        var databaseToSave = !string.IsNullOrEmpty(entry.Database) ? entry.Database : SelectedDatabase ?? string.Empty;

        if (entry.SavedQuery == null && _configuration != null)
        {
            var saved = new SavedQuery
            {
                Name = $"Query {_configuration.SavedQueries.Count + 1}",
                Database = databaseToSave,
                Container = entry.Container,
                Query = entry.QueryText
            };
            _configuration.SavedQueries.Add(saved);
            entry.SavedQuery = saved;
            entry.Database = databaseToSave;
        }
        else if (entry.SavedQuery != null)
        {
            entry.SavedQuery.Database = databaseToSave;
            entry.SavedQuery.Query = entry.QueryText;
            entry.SavedQuery.Container = entry.Container;
            entry.SavedQuery.LastModified = DateTime.UtcNow;
            entry.Database = databaseToSave;
        }

        StatusMessage = "Query saved.";
    }

    [RelayCommand]
    private void DeleteHistory(QueryResultEntry entry)
    {
        if (entry.SavedQuery != null)
        {
            _configuration?.SavedQueries.Remove(entry.SavedQuery);
        }

        QueryHistory.Remove(entry);
        StatusMessage = "Query deleted.";
    }

    [RelayCommand]
    private void AddNewQuery((string database, string container) defaults)
    {
        var newEntry = new QueryResultEntry
        {
            Database = defaults.database,
            Container = defaults.container,
            QueryText = "SELECT * FROM c",
            ResultsJson = string.Empty,
            ResultCount = 0,
            RequestCharge = 0,
            ExecutedAtUtc = DateTime.UtcNow
        };

        QueryHistory.Insert(0, newEntry);
        StatusMessage = "New query added. Edit and click Run to execute.";
    }
}

