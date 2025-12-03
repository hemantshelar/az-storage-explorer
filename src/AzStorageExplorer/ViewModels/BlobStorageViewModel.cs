using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using AzStorageExplorer.Services;

namespace AzStorageExplorer.ViewModels;

public partial class BlobStorageViewModel : ObservableObject
{
    private readonly IBlobStorageService _blobService;
    private string _connectionString = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _containers = new();

    [ObservableProperty]
    private string? _selectedContainer;

    [ObservableProperty]
    private ObservableCollection<BlobItem> _blobs = new();

    [ObservableProperty]
    private BlobItem? _selectedBlob;

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _breadcrumbs = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _blobContent = string.Empty;

    [ObservableProperty]
    private BlobProperties? _selectedBlobProperties;

    public BlobStorageViewModel()
    {
        _blobService = App.Services.GetRequiredService<IBlobStorageService>();
    }

    public void Initialize(string connectionString)
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
            var success = await _blobService.TestConnectionAsync(_connectionString);
            if (success)
            {
                IsConnected = true;
                StatusMessage = "Connected successfully";
                await LoadContainersAsync();
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
    private async Task LoadContainersAsync()
    {
        if (!IsConnected) return;

        IsLoading = true;
        try
        {
            var containers = await _blobService.GetContainersAsync(_connectionString);
            // Assign new collection to trigger PropertyChanged
            Containers = new ObservableCollection<string>(containers);
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

    partial void OnSelectedContainerChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            CurrentPath = string.Empty;
            UpdateBreadcrumbs();
            _ = LoadBlobsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadBlobsAsync()
    {
        if (!IsConnected || string.IsNullOrEmpty(SelectedContainer)) return;

        IsLoading = true;
        try
        {
            var prefix = string.IsNullOrEmpty(CurrentPath) ? null : CurrentPath + "/";
            var blobs = await _blobService.GetBlobsAsync(_connectionString, SelectedContainer, prefix);
            
            Blobs.Clear();
            
            // Add parent directory if not at root
            if (!string.IsNullOrEmpty(CurrentPath))
            {
                Blobs.Add(new BlobItem { Name = "..", IsDirectory = true });
            }
            
            foreach (var blob in blobs.OrderByDescending(b => b.IsDirectory).ThenBy(b => b.Name))
            {
                Blobs.Add(blob);
            }
            
            StatusMessage = $"Loaded {blobs.Count} items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading blobs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToAsync(BlobItem item)
    {
        if (item.IsDirectory)
        {
            if (item.Name == "..")
            {
                // Go up one level
                var lastSlash = CurrentPath.LastIndexOf('/');
                CurrentPath = lastSlash > 0 ? CurrentPath[..lastSlash] : string.Empty;
            }
            else
            {
                // Navigate into directory
                CurrentPath = string.IsNullOrEmpty(CurrentPath) 
                    ? item.Name 
                    : $"{CurrentPath}/{item.Name}";
            }
            
            UpdateBreadcrumbs();
            await LoadBlobsAsync();
        }
        else
        {
            // Select blob and load preview
            SelectedBlob = item;
            await LoadBlobPreviewAsync();
        }
    }

    [RelayCommand]
    private async Task NavigateToBreadcrumbAsync(int index)
    {
        if (index < 0)
        {
            CurrentPath = string.Empty;
        }
        else
        {
            var parts = CurrentPath.Split('/');
            CurrentPath = string.Join("/", parts.Take(index + 1));
        }
        
        UpdateBreadcrumbs();
        await LoadBlobsAsync();
    }

    private void UpdateBreadcrumbs()
    {
        Breadcrumbs.Clear();
        Breadcrumbs.Add(SelectedContainer ?? "Root");
        
        if (!string.IsNullOrEmpty(CurrentPath))
        {
            foreach (var part in CurrentPath.Split('/'))
            {
                Breadcrumbs.Add(part);
            }
        }
    }

    [RelayCommand]
    private async Task LoadBlobPreviewAsync()
    {
        if (SelectedBlob == null || SelectedBlob.IsDirectory || string.IsNullOrEmpty(SelectedContainer))
            return;

        IsLoading = true;
        try
        {
            var blobPath = string.IsNullOrEmpty(CurrentPath) 
                ? SelectedBlob.Name 
                : $"{CurrentPath}/{SelectedBlob.Name}";
            
            SelectedBlobProperties = await _blobService.GetBlobPropertiesAsync(
                _connectionString, SelectedContainer, blobPath);

            // Only load text content for small text files
            if (SelectedBlobProperties.Size < 1024 * 1024 && // Less than 1MB
                IsTextContentType(SelectedBlobProperties.ContentType))
            {
                BlobContent = await _blobService.GetBlobContentAsStringAsync(
                    _connectionString, SelectedContainer, blobPath);
            }
            else
            {
                BlobContent = $"[Binary file or too large to preview. Size: {FormatFileSize(SelectedBlobProperties.Size)}]";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading blob: {ex.Message}";
            BlobContent = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool IsTextContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;
        
        return contentType.StartsWith("text/") ||
               contentType.Contains("json") ||
               contentType.Contains("xml") ||
               contentType.Contains("javascript");
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}

