using AzStorageExplorer.Models;

namespace AzStorageExplorer.Services;

public interface IProjectService
{
    Task<ProjectConfiguration> LoadProjectAsync(string filePath);
    Task SaveProjectAsync(string filePath, ProjectConfiguration configuration);
    Task<ProjectConfiguration> CreateNewProjectAsync(string name, List<StorageType> types);
    bool ValidateConfiguration(ProjectConfiguration configuration);
}

