using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using AzStorageExplorer.Models;
using AzStorageExplorer.Services;

namespace AzStorageExplorer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IProjectService _projectService;

    [ObservableProperty]
    private ObservableCollection<ProjectViewModel> _openProjects = new();

    [ObservableProperty]
    private ProjectViewModel? _selectedProject;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public MainViewModel(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [RelayCommand]
    private async Task CreateNewProjectAsync()
    {
        var config = await _projectService.CreateNewProjectAsync(
            $"New Project {OpenProjects.Count + 1}",
            new List<StorageType> { StorageType.CosmosDb });
        
        var projectVm = new ProjectViewModel(config, null);
        OpenProjects.Add(projectVm);
        SelectedProject = projectVm;
        StatusMessage = $"Created new project: {config.Name}";
    }

    [RelayCommand]
    private async Task OpenProjectAsync(string filePath)
    {
        try
        {
            var config = await _projectService.LoadProjectAsync(filePath);
            var projectVm = new ProjectViewModel(config, filePath);
            OpenProjects.Add(projectVm);
            SelectedProject = projectVm;
            StatusMessage = $"Opened project: {config.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening project: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        if (SelectedProject == null) return;

        try
        {
            if (string.IsNullOrEmpty(SelectedProject.FilePath))
            {
                // Need to prompt for save location
                StatusMessage = "Please specify a save location";
                return;
            }

            await _projectService.SaveProjectAsync(SelectedProject.FilePath, SelectedProject.Configuration);
            SelectedProject.IsDirty = false;
            StatusMessage = $"Saved project: {SelectedProject.Configuration.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving project: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseProject(ProjectViewModel project)
    {
        OpenProjects.Remove(project);
        if (SelectedProject == project)
        {
            SelectedProject = OpenProjects.FirstOrDefault();
        }
        StatusMessage = "Project closed";
    }
}

