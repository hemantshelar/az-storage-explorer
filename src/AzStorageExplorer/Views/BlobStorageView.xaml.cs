using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using AzStorageExplorer.Services;
using AzStorageExplorer.ViewModels;

namespace AzStorageExplorer.Views;

public sealed partial class BlobStorageView : UserControl
{
    private readonly BlobStorageViewModel _viewModel;
    
    public event EventHandler<string>? ConnectionStringChanged;

    public BlobStorageView()
    {
        this.InitializeComponent();
        _viewModel = new BlobStorageViewModel();
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
                case nameof(_viewModel.Containers):
                    ContainerComboBox.ItemsSource = _viewModel.Containers;
                    break;
                case nameof(_viewModel.SelectedContainer):
                    ContainerComboBox.SelectedItem = _viewModel.SelectedContainer;
                    break;
                case nameof(_viewModel.Blobs):
                    BlobsList.ItemsSource = _viewModel.Blobs;
                    break;
                case nameof(_viewModel.Breadcrumbs):
                    PathBreadcrumb.ItemsSource = _viewModel.Breadcrumbs;
                    break;
                case nameof(_viewModel.IsLoading):
                    LoadingRing.IsActive = _viewModel.IsLoading;
                    break;
                case nameof(_viewModel.StatusMessage):
                    StatusText.Text = _viewModel.StatusMessage;
                    break;
                case nameof(_viewModel.BlobContent):
                    ContentPreviewText.Text = _viewModel.BlobContent;
                    break;
                case nameof(_viewModel.SelectedBlobProperties):
                    UpdatePropertiesPanel();
                    break;
            }
        });
    }

    private void UpdatePropertiesPanel()
    {
        var props = _viewModel.SelectedBlobProperties;
        if (props != null)
        {
            PropertiesPanel.Visibility = Visibility.Visible;
            BlobNameText.Text = props.Name;
            BlobSizeText.Text = FormatFileSize(props.Size);
            BlobTypeText.Text = props.ContentType ?? "Unknown";
            BlobModifiedText.Text = props.LastModified?.LocalDateTime.ToString("g") ?? "Unknown";
        }
        else
        {
            PropertiesPanel.Visibility = Visibility.Collapsed;
        }
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

    private void ConnectionStringBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ConnectionStringChanged?.Invoke(this, ConnectionStringBox.Text);
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Initialize(ConnectionStringBox.Text);
        await _viewModel.ConnectCommand.ExecuteAsync(null);
    }

    private void ContainerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ContainerComboBox.SelectedItem is string selectedContainer)
        {
            _viewModel.SelectedContainer = selectedContainer;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadBlobsCommand.ExecuteAsync(null);
    }

    private void BlobsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BlobsList.SelectedItem is BlobItem item && !item.IsDirectory)
        {
            _viewModel.SelectedBlob = item;
            _ = _viewModel.LoadBlobPreviewCommand.ExecuteAsync(null);
        }
    }

    private async void BlobsList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (BlobsList.SelectedItem is BlobItem item)
        {
            await _viewModel.NavigateToCommand.ExecuteAsync(item);
        }
    }

    private async void PathBreadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        await _viewModel.NavigateToBreadcrumbCommand.ExecuteAsync(args.Index - 1);
    }
}

