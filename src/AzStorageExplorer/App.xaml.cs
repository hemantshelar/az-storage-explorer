using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using AzStorageExplorer.Services;
using AzStorageExplorer.ViewModels;

namespace AzStorageExplorer;

public partial class App : Application
{
    private Window? _mainWindow;
    
    public static IServiceProvider Services { get; private set; } = null!;
    
    public App()
    {
        this.InitializeComponent();
        Services = ConfigureServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
    
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Register services
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<ICosmosDbService, CosmosDbService>();
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddSingleton<ITableStorageService, TableStorageService>();
        services.AddSingleton<IQueueStorageService, QueueStorageService>();
        
        // Register ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ProjectViewModel>();
        services.AddTransient<CosmosDbViewModel>();
        services.AddTransient<BlobStorageViewModel>();
        services.AddTransient<TableStorageViewModel>();
        services.AddTransient<QueueStorageViewModel>();
        
        return services.BuildServiceProvider();
    }
}

