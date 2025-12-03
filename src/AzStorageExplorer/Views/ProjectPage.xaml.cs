using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.Storage;
using AzStorageExplorer.Models;
using AzStorageExplorer.Services;
using AzStorageExplorer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using WinRT.Interop;

namespace AzStorageExplorer.Views;

public sealed partial class ProjectPage : UserControl
{
    public ProjectViewModel ViewModel { get; }
    private readonly IProjectService _projectService;
    private string? _filePath;

    public ProjectPage(ProjectConfiguration configuration, string? filePath)
    {
        this.InitializeComponent();
        
        ViewModel = new ProjectViewModel(configuration, filePath);
        _projectService = App.Services.GetRequiredService<IProjectService>();
        _filePath = filePath;
        
        DataContext = ViewModel;
        
        InitializeStorageTypeChecks();
        UpdateNavigationVisibility();
        
        // Navigate to first available storage type
        if (CosmosDbNavItem.Visibility == Visibility.Visible)
        {
            StorageNavigation.SelectedItem = CosmosDbNavItem;
        }
    }

    private void InitializeStorageTypeChecks()
    {
        CosmosDbCheck.IsChecked = ViewModel.Configuration.Types.Contains(StorageType.CosmosDb);
        BlobCheck.IsChecked = ViewModel.Configuration.Types.Contains(StorageType.Blob);
        TableCheck.IsChecked = ViewModel.Configuration.Types.Contains(StorageType.Table);
        QueueCheck.IsChecked = ViewModel.Configuration.Types.Contains(StorageType.Queue);
    }

    private void StorageTypeCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.Tag is string tagValue)
        {
            if (Enum.TryParse<StorageType>(tagValue, out var storageType))
            {
                ViewModel.ToggleStorageTypeCommand.Execute(storageType);
                UpdateNavigationVisibility();
            }
        }
    }

    private void UpdateNavigationVisibility()
    {
        CosmosDbNavItem.Visibility = ViewModel.Configuration.Types.Contains(StorageType.CosmosDb) 
            ? Visibility.Visible : Visibility.Collapsed;
        BlobNavItem.Visibility = ViewModel.Configuration.Types.Contains(StorageType.Blob) 
            ? Visibility.Visible : Visibility.Collapsed;
        TableNavItem.Visibility = ViewModel.Configuration.Types.Contains(StorageType.Table) 
            ? Visibility.Visible : Visibility.Collapsed;
        QueueNavItem.Visibility = ViewModel.Configuration.Types.Contains(StorageType.Queue) 
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void StorageNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            if (Enum.TryParse<StorageType>(tag, out var storageType))
            {
                NavigateToStorageView(storageType);
            }
        }
    }

    private void NavigateToStorageView(StorageType type)
    {
        switch (type)
        {
            case StorageType.CosmosDb:
                var cosmosView = new CosmosDbView();
                cosmosView.Initialize(ViewModel.Configuration.CosmosDb ?? new CosmosDbConfiguration(), 
                    ViewModel.GetConnectionString(StorageType.CosmosDb));
                cosmosView.ConnectionStringChanged += (s, cs) => ViewModel.UpdateConnectionString(StorageType.CosmosDb, cs);
                ContentFrame.Content = cosmosView;
                break;
                
            case StorageType.Blob:
                var blobView = new BlobStorageView();
                blobView.Initialize(ViewModel.GetConnectionString(StorageType.Blob));
                blobView.ConnectionStringChanged += (s, cs) => ViewModel.UpdateConnectionString(StorageType.Blob, cs);
                ContentFrame.Content = blobView;
                break;
                
            case StorageType.Table:
                var tableView = new TableStorageView();
                tableView.Initialize(ViewModel.Configuration.TableStorage ?? new TableStorageConfiguration(),
                    ViewModel.GetConnectionString(StorageType.Table));
                tableView.ConnectionStringChanged += (s, cs) => ViewModel.UpdateConnectionString(StorageType.Table, cs);
                ContentFrame.Content = tableView;
                break;
                
            case StorageType.Queue:
                var queueView = new QueueStorageView();
                queueView.Initialize(ViewModel.GetConnectionString(StorageType.Queue));
                queueView.ConnectionStringChanged += (s, cs) => ViewModel.UpdateConnectionString(StorageType.Queue, cs);
                ContentFrame.Content = queueView;
                break;
        }
    }

    private void ProjectNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.IsDirty = true;
    }

    public async Task SaveProjectAsync(Window parentWindow)
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            var picker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(parentWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
            
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("JSON Project File", new List<string> { ".json" });
            picker.SuggestedFileName = ViewModel.Configuration.Name;
            
            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                _filePath = file.Path;
                ViewModel.FilePath = _filePath;
            }
            else
            {
                return;
            }
        }

        await _projectService.SaveProjectAsync(_filePath, ViewModel.Configuration);
        ViewModel.IsDirty = false;
    }
}

