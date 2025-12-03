using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AzStorageExplorer.Models;

public partial class QueryResultEntry : ObservableObject
{
    [ObservableProperty]
    private string _database = string.Empty;
    
    [ObservableProperty]
    private string _container = string.Empty;
    
    [ObservableProperty]
    private string _queryText = string.Empty;

    // Optional link to a persisted SavedQuery in the configuration
    public SavedQuery? SavedQuery { get; set; }

    [ObservableProperty]
    private string _resultsJson = string.Empty;
    
    [ObservableProperty]
    private int _resultCount;
    
    [ObservableProperty]
    private double _requestCharge;

    [ObservableProperty]
    private DateTime _executedAtUtc = DateTime.UtcNow;

    // Table view support
    [ObservableProperty]
    private bool _canShowAsTable;
    
    [ObservableProperty]
    private bool _showAsTable;
    
    [ObservableProperty]
    private List<string> _tableColumns = new();
    
    // Each row is a list of values in column order
    [ObservableProperty]
    private List<TableRow> _tableRows = new();

    public string ExecutedAtLocal =>
        ExecutedAtUtc.ToLocalTime().ToString("G");

    public string RequestChargeFormatted =>
        RequestCharge.ToString("F2");
    
    // Computed properties for view visibility
    public bool ShowJsonView => !ShowAsTable;
    public bool ShowTableView => ShowAsTable && CanShowAsTable;
    
    partial void OnShowAsTableChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowJsonView));
        OnPropertyChanged(nameof(ShowTableView));
    }
}

/// <summary>
/// Represents a single row in the table view.
/// </summary>
public class TableRow
{
    public List<string> Values { get; set; } = new();
}
