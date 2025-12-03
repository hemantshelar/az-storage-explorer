using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using AzStorageExplorer.Services;

namespace AzStorageExplorer.ViewModels;

public partial class QueueStorageViewModel : ObservableObject
{
    private readonly IQueueStorageService _queueService;
    private string _connectionString = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _queues = new();

    [ObservableProperty]
    private string? _selectedQueue;

    [ObservableProperty]
    private ObservableCollection<QueueMessage> _messages = new();

    [ObservableProperty]
    private QueueMessage? _selectedMessage;

    [ObservableProperty]
    private QueueProperties? _queueProperties;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public QueueStorageViewModel()
    {
        _queueService = App.Services.GetRequiredService<IQueueStorageService>();
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
            var success = await _queueService.TestConnectionAsync(_connectionString);
            if (success)
            {
                IsConnected = true;
                StatusMessage = "Connected successfully";
                await LoadQueuesAsync();
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
    private async Task LoadQueuesAsync()
    {
        if (!IsConnected) return;

        IsLoading = true;
        try
        {
            var queues = await _queueService.GetQueuesAsync(_connectionString);
            // Assign new collection to trigger PropertyChanged
            Queues = new ObservableCollection<string>(queues);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading queues: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedQueueChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadQueueDetailsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadQueueDetailsAsync()
    {
        if (!IsConnected || string.IsNullOrEmpty(SelectedQueue)) return;

        IsLoading = true;
        try
        {
            // Load queue properties
            QueueProperties = await _queueService.GetQueuePropertiesAsync(_connectionString, SelectedQueue);
            
            // Load messages
            await PeekMessagesAsync();
            
            StatusMessage = $"Queue: {SelectedQueue} - {QueueProperties.ApproximateMessagesCount} messages";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading queue details: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PeekMessagesAsync()
    {
        if (!IsConnected || string.IsNullOrEmpty(SelectedQueue)) return;

        IsLoading = true;
        try
        {
            var messages = await _queueService.PeekMessagesAsync(_connectionString, SelectedQueue);
            Messages.Clear();
            foreach (var message in messages)
            {
                Messages.Add(message);
            }
            StatusMessage = $"Peeked {messages.Count} messages";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error peeking messages: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadQueueDetailsAsync();
    }
}

