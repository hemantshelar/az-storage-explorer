using System;
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

    public string ExecutedAtLocal =>
        ExecutedAtUtc.ToLocalTime().ToString("G");

    public string RequestChargeFormatted =>
        RequestCharge.ToString("F2");
}


