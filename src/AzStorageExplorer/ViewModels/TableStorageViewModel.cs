using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Text.Json;
using AzStorageExplorer.Models;
using AzStorageExplorer.Services;

namespace AzStorageExplorer.ViewModels;

public partial class TableStorageViewModel : ObservableObject
{
    private readonly ITableStorageService _tableService;
    private TableStorageConfiguration? _configuration;
    private string _connectionString = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _tables = new();

    [ObservableProperty]
    private string? _selectedTable;

    [ObservableProperty]
    private string _filterQuery = string.Empty;

    [ObservableProperty]
    private string _queryResults = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Dictionary<string, object>> _entities = new();

    [ObservableProperty]
    private Dictionary<string, object>? _selectedEntity;

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
    private int _resultCount;

    public TableStorageViewModel()
    {
        _tableService = App.Services.GetRequiredService<ITableStorageService>();
    }

    public void Initialize(TableStorageConfiguration configuration, string connectionString)
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
            var success = await _tableService.TestConnectionAsync(_connectionString);
            if (success)
            {
                IsConnected = true;
                StatusMessage = "Connected successfully";
                await LoadTablesAsync();
            }
            else
            {
                StatusMessage = "Connection failed";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadTablesAsync()
    {
        if (!IsConnected) return;

        IsLoading = true;
        try
        {
            var tables = await _tableService.GetTablesAsync(_connectionString);
            // Assign new collection to trigger PropertyChanged
            Tables = new ObservableCollection<string>(tables);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading tables: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedTableChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = QueryEntitiesAsync();
        }
    }

    [RelayCommand]
    private async Task QueryEntitiesAsync()
    {
        if (!IsConnected || string.IsNullOrEmpty(SelectedTable)) return;

        IsLoading = true;
        StatusMessage = "Querying...";

        try
        {
            var filter = string.IsNullOrWhiteSpace(FilterQuery) ? null : FilterQuery;
            var results = await _tableService.QueryEntitiesAsync(_connectionString, SelectedTable, filter);
            
            Entities.Clear();
            foreach (var entity in results)
            {
                Entities.Add(entity);
            }
            
            ResultCount = results.Count;
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            QueryResults = JsonSerializer.Serialize(results, options);
            StatusMessage = $"Query completed. {ResultCount} results.";
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
    private void SaveQuery(string queryName)
    {
        if (string.IsNullOrWhiteSpace(queryName))
            return;

        var query = new SavedQuery
        {
            Name = queryName,
            Container = SelectedTable ?? string.Empty,
            Query = FilterQuery
        };

        SavedQueries.Add(query);
        _configuration?.SavedQueries.Add(query);
        StatusMessage = $"Query saved: {queryName}";
    }

    [RelayCommand]
    private void LoadQuery(SavedQuery query)
    {
        FilterQuery = query.Query;
        if (!string.IsNullOrEmpty(query.Container) && Tables.Contains(query.Container))
        {
            SelectedTable = query.Container;
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
}

