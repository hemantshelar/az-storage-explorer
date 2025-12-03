using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.Storage;
using AzStorageExplorer.Services;
using AzStorageExplorer.ViewModels;
using AzStorageExplorer.Views;
using AzStorageExplorer.Models;
using WinRT.Interop;

namespace AzStorageExplorer;

public sealed partial class MainWindow : Window
{
    private readonly IProjectService _projectService;
    private int _tabCounter = 0;

    public MainWindow()
    {
        this.InitializeComponent();
        
        _projectService = App.Services.GetRequiredService<IProjectService>();
        
        // Set window size
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
        
        // Create welcome tab
        CreateWelcomeTab();
    }

    private void CreateWelcomeTab()
    {
        var welcomeTab = new TabViewItem
        {
            Header = "Welcome",
            IconSource = new SymbolIconSource { Symbol = Symbol.Home },
            Content = CreateWelcomeContent()
        };
        
        ProjectTabView.TabItems.Add(welcomeTab);
        ProjectTabView.SelectedItem = welcomeTab;
    }

    private UIElement CreateWelcomeContent()
    {
        var grid = new Grid
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppBackgroundBrush"],
            Padding = new Thickness(40)
        };
        
        var stackPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 24
        };
        
        var titleStack = new StackPanel { Spacing = 8, HorizontalAlignment = HorizontalAlignment.Center };
        titleStack.Children.Add(new FontIcon 
        { 
            Glyph = "\uE753", 
            FontSize = 64,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppHighlightBrush"]
        });
        titleStack.Children.Add(new TextBlock 
        { 
            Text = "Azure Storage Explorer", 
            FontSize = 32, 
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppTextBrush"]
        });
        titleStack.Children.Add(new TextBlock 
        { 
            Text = "Browse and manage your Azure Storage and Cosmos DB resources", 
            FontSize = 14,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AppSecondaryTextBrush"]
        });
        
        var buttonStack = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var newButton = new Button 
        { 
            Content = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Spacing = 8,
                Children = 
                {
                    new FontIcon { Glyph = "\uE710", FontSize = 16 },
                    new TextBlock { Text = "New Project", VerticalAlignment = VerticalAlignment.Center }
                }
            },
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
            Padding = new Thickness(24, 12, 24, 12)
        };
        newButton.Click += NewProjectButton_Click;
        
        var openButton = new Button 
        { 
            Content = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Spacing = 8,
                Children = 
                {
                    new FontIcon { Glyph = "\uE838", FontSize = 16 },
                    new TextBlock { Text = "Open Project", VerticalAlignment = VerticalAlignment.Center }
                }
            },
            Padding = new Thickness(24, 12, 24, 12)
        };
        openButton.Click += OpenProjectButton_Click;
        
        buttonStack.Children.Add(newButton);
        buttonStack.Children.Add(openButton);
        
        stackPanel.Children.Add(titleStack);
        stackPanel.Children.Add(buttonStack);
        
        grid.Children.Add(stackPanel);
        return grid;
    }

    private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var config = new ProjectConfiguration
        {
            Name = $"New Project {++_tabCounter}",
            Types = new List<StorageType> { StorageType.CosmosDb }
        };
        
        CreateProjectTab(config, null);
        UpdateStatus("New project created");
    }

    private async void OpenProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);
        
        picker.FileTypeFilter.Add(".json");
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        
        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            try
            {
                var config = await _projectService.LoadProjectAsync(file.Path);
                CreateProjectTab(config, file.Path);
                UpdateStatus($"Loaded project: {config.Name}");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Error Loading Project", ex.Message);
            }
        }
    }

    private async void SaveProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProjectTabView.SelectedItem is TabViewItem tab && tab.Content is ProjectPage projectPage)
        {
            await projectPage.SaveProjectAsync(this);
            UpdateStatus("Project saved");
        }
    }

    private void CreateProjectTab(ProjectConfiguration config, string? filePath)
    {
        var projectPage = new ProjectPage(config, filePath);
        
        var tab = new TabViewItem
        {
            Header = config.Name,
            IconSource = new SymbolIconSource { Symbol = Symbol.Document },
            Content = projectPage
        };
        
        ProjectTabView.TabItems.Add(tab);
        ProjectTabView.SelectedItem = tab;
    }

    private void TabView_AddTabButtonClick(TabView sender, object args)
    {
        NewProjectButton_Click(sender, new RoutedEventArgs());
    }

    private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        sender.TabItems.Remove(args.Tab);
        
        if (sender.TabItems.Count == 0)
        {
            CreateWelcomeTab();
        }
    }

    private void UpdateStatus(string message)
    {
        StatusText.Text = message;
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }
}

