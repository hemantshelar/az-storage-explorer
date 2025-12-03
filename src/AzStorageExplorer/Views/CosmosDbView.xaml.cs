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
    public event EventHandler? QuerySaved;
    public event EventHandler? QueryDeleted;

    public CosmosDbView()
    {
        this.InitializeComponent();
        _viewModel = new CosmosDbViewModel();
        this.DataContext = _viewModel;

        // Bind ViewModel properties to UI
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public void Initialize(CosmosDbConfiguration configuration, string connectionString)
    {
        _viewModel.Initialize(configuration, connectionString);
        ConnectionStringBox.Text = connectionString;
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
                case nameof(_viewModel.IsLoading):
                    LoadingRing.IsActive = _viewModel.IsLoading;
                    break;
                case nameof(_viewModel.StatusMessage):
                    StatusText.Text = _viewModel.StatusMessage;
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
        _viewModel.UpdateConnectionString(ConnectionStringBox.Text);
        await _viewModel.ConnectCommand.ExecuteAsync(null);
    }

    private void DatabaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DatabaseComboBox.SelectedItem is string selectedDb)
        {
            _viewModel.SelectedDatabase = selectedDb;
        }
    }

    private void NewQueryButton_Click(object sender, RoutedEventArgs e)
    {
        // Get currently selected database and container as defaults
        var database = DatabaseComboBox.SelectedItem as string ?? string.Empty;
        var container = ContainerComboBox.SelectedItem as string ?? string.Empty;
        _viewModel.AddNewQueryCommand.Execute((database, container));
    }

    private async void RunHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is QueryResultEntry entry)
        {
            await _viewModel.RunFromHistoryCommand.ExecuteAsync(entry);
        }
    }

    private void SaveHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is QueryResultEntry entry)
        {
            _viewModel.SaveHistoryCommand.Execute(entry);
            QuerySaved?.Invoke(this, EventArgs.Empty);
        }
    }

    private void DeleteHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is QueryResultEntry entry)
        {
            _viewModel.DeleteHistoryCommand.Execute(entry);
            QueryDeleted?.Invoke(this, EventArgs.Empty);
        }
    }
}

