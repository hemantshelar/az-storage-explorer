using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AzStorageExplorer.Models;

/// <summary>
/// Represents a group of queries for a specific container.
/// </summary>
public partial class ContainerQueryGroup : ObservableObject
{
    [ObservableProperty]
    private string _containerName = string.Empty;
    
    [ObservableProperty]
    private bool _isExpanded = false;
    
    [ObservableProperty]
    private ObservableCollection<QueryResultEntry> _queries = new();
    
    public int QueryCount => Queries.Count;
    
    public ContainerQueryGroup(string containerName)
    {
        _containerName = containerName;
    }
}

