using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AzStorageExplorer.Models;
using AzStorageExplorer.ViewModels;

namespace AzStorageExplorer.Views;

public sealed partial class TableStorageView : UserControl
{
    private readonly TableStorageViewModel _viewModel;
    
    public event EventHandler<string>? ConnectionStringChanged;

    public TableStorageView()
    {
        this.InitializeComponent();
        _viewModel = new TableStorageViewModel();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public void Initialize(TableStorageConfiguration configuration, string connectionString)
    {
        _viewModel.Initialize(configuration, connectionString);
        ConnectionStringBox.Text = connectionString;
        SavedQueriesList.ItemsSource = _viewModel.SavedQueries;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Tables):
                    TableComboBox.ItemsSource = _viewModel.Tables;
                    break;
                case nameof(_viewModel.SelectedTable):
                    TableComboBox.SelectedItem = _viewModel.SelectedTable;
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
                    ResultCountText.Text = $"{_viewModel.ResultCount} entities";
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
            ? new TableStorageConfiguration { SavedQueries = _viewModel.SavedQueries.ToList() } 
            : new TableStorageConfiguration(), ConnectionStringBox.Text);
        
        await _viewModel.ConnectCommand.ExecuteAsync(null);
    }

    private void TableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TableComboBox.SelectedItem is string selectedTable)
        {
            _viewModel.SelectedTable = selectedTable;
        }
    }

    private async void QueryButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.FilterQuery = FilterTextBox.Text;
        await _viewModel.QueryEntitiesCommand.ExecuteAsync(null);
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
            _viewModel.FilterQuery = FilterTextBox.Text;
            _viewModel.SaveQueryCommand.Execute(inputBox.Text);
        }
    }

    private void SavedQueriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SavedQueriesList.SelectedItem is SavedQuery query)
        {
            _viewModel.LoadQueryCommand.Execute(query);
            FilterTextBox.Text = query.Query;
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

