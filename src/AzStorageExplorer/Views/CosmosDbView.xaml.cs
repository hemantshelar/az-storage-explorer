using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AzStorageExplorer.Models;
using AzStorageExplorer.ViewModels;

// Explicitly use our SavedQuery model

namespace AzStorageExplorer.Views;

public sealed partial class CosmosDbView : UserControl
{
    private readonly CosmosDbViewModel _viewModel;
    
    public event EventHandler<string>? ConnectionStringChanged;

    public CosmosDbView()
    {
        this.InitializeComponent();
        _viewModel = new CosmosDbViewModel();
        
        // Bind ViewModel properties to UI
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public void Initialize(CosmosDbConfiguration configuration, string connectionString)
    {
        _viewModel.Initialize(configuration, connectionString);
        ConnectionStringBox.Text = connectionString;
        
        // Update saved queries list
        SavedQueriesList.ItemsSource = _viewModel.SavedQueries;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Databases):
                    DatabaseComboBox.ItemsSource = _viewModel.Databases;
                    break;
                case nameof(_viewModel.SelectedDatabase):
                    DatabaseComboBox.SelectedItem = _viewModel.SelectedDatabase;
                    break;
                case nameof(_viewModel.Containers):
                    ContainerComboBox.ItemsSource = _viewModel.Containers;
                    break;
                case nameof(_viewModel.SelectedContainer):
                    ContainerComboBox.SelectedItem = _viewModel.SelectedContainer;
                    break;
                case nameof(_viewModel.QueryResults):
                    ResultsTextBlock.Text = _viewModel.QueryResults;
                    break;
                case nameof(_viewModel.IsLoading):
                    LoadingRing.IsActive = _viewModel.IsLoading;
                    break;
                case nameof(_viewModel.StatusMessage):
                    StatusText.Text = _viewModel.StatusMessage;
                    break;
                case nameof(_viewModel.ResultCount):
                    ResultCountText.Text = $"{_viewModel.ResultCount} items";
                    break;
                case nameof(_viewModel.LastRequestCharge):
                    RequestChargeText.Text = $"RU: {_viewModel.LastRequestCharge:F2}";
                    break;
            }
        });
    }

    private void ConnectionStringBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ConnectionStringChanged?.Invoke(this, ConnectionStringBox.Text);
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Initialize(_viewModel.SavedQueries.Any() 
            ? new CosmosDbConfiguration { SavedQueries = _viewModel.SavedQueries.ToList() } 
            : new CosmosDbConfiguration(), ConnectionStringBox.Text);
        
        await _viewModel.ConnectCommand.ExecuteAsync(null);
    }

    private void DatabaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DatabaseComboBox.SelectedItem is string selectedDb)
        {
            _viewModel.SelectedDatabase = selectedDb;
        }
    }

    private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.QueryText = QueryTextBox.Text;
        _viewModel.SelectedContainer = ContainerComboBox.SelectedItem as string;
        await _viewModel.ExecuteQueryCommand.ExecuteAsync(null);
    }

    private async void SaveQueryButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Save Query",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var inputBox = new TextBox
        {
            PlaceholderText = "Enter query name...",
            Margin = new Thickness(0, 12, 0, 0)
        };
        dialog.Content = inputBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputBox.Text))
        {
            _viewModel.QueryText = QueryTextBox.Text;
            _viewModel.SelectedContainer = ContainerComboBox.SelectedItem as string;
            _viewModel.SaveQueryCommand.Execute(inputBox.Text);
        }
    }

    private void SavedQueriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SavedQueriesList.SelectedItem is SavedQuery query)
        {
            _viewModel.LoadQueryCommand.Execute(query);
            QueryTextBox.Text = query.Query;
            
            // Select the container if available
            if (!string.IsNullOrEmpty(query.Container))
            {
                ContainerComboBox.SelectedItem = query.Container;
            }
        }
    }

    private void DeleteQueryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is SavedQuery query)
        {
            _viewModel.DeleteQueryCommand.Execute(query);
        }
    }
}

