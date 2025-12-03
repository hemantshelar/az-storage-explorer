using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AzStorageExplorer.Services;
using AzStorageExplorer.ViewModels;

namespace AzStorageExplorer.Views;

public sealed partial class QueueStorageView : UserControl
{
    private readonly QueueStorageViewModel _viewModel;
    
    public event EventHandler<string>? ConnectionStringChanged;

    public QueueStorageView()
    {
        this.InitializeComponent();
        _viewModel = new QueueStorageViewModel();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public void Initialize(string connectionString)
    {
        _viewModel.Initialize(connectionString);
        ConnectionStringBox.Text = connectionString;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Queues):
                    QueueComboBox.ItemsSource = _viewModel.Queues;
                    break;
                case nameof(_viewModel.SelectedQueue):
                    QueueComboBox.SelectedItem = _viewModel.SelectedQueue;
                    break;
                case nameof(_viewModel.Messages):
                    MessagesList.ItemsSource = _viewModel.Messages;
                    break;
                case nameof(_viewModel.IsLoading):
                    LoadingRing.IsActive = _viewModel.IsLoading;
                    break;
                case nameof(_viewModel.StatusMessage):
                    StatusText.Text = _viewModel.StatusMessage;
                    break;
                case nameof(_viewModel.QueueProperties):
                    UpdateQueueInfo();
                    break;
                case nameof(_viewModel.SelectedMessage):
                    UpdateMessageDetails();
                    break;
            }
        });
    }

    private void UpdateQueueInfo()
    {
        var props = _viewModel.QueueProperties;
        if (props != null)
        {
            QueueCountText.Text = $"~{props.ApproximateMessagesCount} messages in queue";
        }
    }

    private void UpdateMessageDetails()
    {
        var msg = _viewModel.SelectedMessage;
        if (msg != null)
        {
            MessagePropertiesPanel.Visibility = Visibility.Visible;
            MessageIdText.Text = msg.MessageId;
            MessageInsertedText.Text = msg.InsertedOn?.LocalDateTime.ToString("g") ?? "Unknown";
            MessageExpiresText.Text = msg.ExpiresOn?.LocalDateTime.ToString("g") ?? "Unknown";
            MessageDequeueText.Text = msg.DequeueCount.ToString();
            MessageContentText.Text = msg.MessageText;
        }
        else
        {
            MessagePropertiesPanel.Visibility = Visibility.Collapsed;
            MessageContentText.Text = string.Empty;
        }
    }

    private void ConnectionStringBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ConnectionStringChanged?.Invoke(this, ConnectionStringBox.Text);
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Initialize(ConnectionStringBox.Text);
        await _viewModel.ConnectCommand.ExecuteAsync(null);
    }

    private void QueueComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QueueComboBox.SelectedItem is string selectedQueue)
        {
            _viewModel.SelectedQueue = selectedQueue;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RefreshCommand.ExecuteAsync(null);
    }

    private void MessagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MessagesList.SelectedItem is QueueMessage message)
        {
            _viewModel.SelectedMessage = message;
        }
    }
}

