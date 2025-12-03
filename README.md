# Azure Storage Explorer

A modern Windows desktop application built with WinUI 3 and Windows App SDK for browsing and querying Azure Storage services.

## Features

- **Multi-Project Support**: Open multiple configuration files in tabs, similar to VS Code
- **Cosmos DB Explorer**: Browse databases, containers, and execute SQL queries with saved query management
- **Blob Storage Browser**: Navigate blob containers with hierarchical folder view and file preview
- **Table Storage Query**: Query Azure Table entities using OData filters
- **Queue Storage Viewer**: Peek queue messages and view message details
- **Configuration Files**: Save connection strings and queries in JSON project files for easy reuse

## Requirements

- Windows 10 version 1809 (build 17763) or later
- .NET 8.0 SDK
- Visual Studio 2022 (17.0+) with Windows App SDK workload

## Getting Started

### Building the Application

1. Clone the repository
2. Open `AzStorageExplorer.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Build and run (F5)

Or using the command line:

```bash
cd src/AzStorageExplorer
dotnet restore
dotnet build
dotnet run
```

### Creating a New Project

1. Click **New** or press `Ctrl+N`
2. Enter a project name
3. Select the storage types you want to use (Cosmos DB, Blob, Table, Queue)
4. Enter connection strings for each service
5. Click **Connect** to start exploring

### Saving Your Configuration

1. Click **Save** or press `Ctrl+S`
2. Choose a location for your `.json` project file
3. The file will store:
   - Project name and description
   - Connection strings
   - Saved queries (for Cosmos DB and Table Storage)

### Loading an Existing Project

1. Click **Open** or press `Ctrl+O`
2. Select a `.json` project file
3. Your configuration and saved queries will be loaded

## Project Structure

```
AzStorageExplorer/
├── Models/              # Data models and configuration classes
├── Services/            # Azure SDK service wrappers
├── ViewModels/          # MVVM view models
├── Views/               # XAML pages and user controls
├── Converters/          # Value converters for XAML bindings
└── Assets/              # Application assets
```

## Configuration File Format

```json
{
  "name": "My Project",
  "type": ["cosmosdb", "blob"],
  "cosmosDb": {
    "connectionString": "AccountEndpoint=...",
    "databaseId": "MyDatabase",
    "savedQueries": [
      {
        "id": "guid",
        "name": "Get Active Users",
        "container": "users",
        "query": "SELECT * FROM c WHERE c.active = true"
      }
    ]
  },
  "blobStorage": {
    "connectionString": "DefaultEndpointsProtocol=..."
  }
}
```

## Technology Stack

| Component        | Technology                    |
| ---------------- | ----------------------------- |
| UI Framework     | WinUI 3 + Windows App SDK 1.5 |
| Target Framework | .NET 8.0                      |
| Architecture     | MVVM (CommunityToolkit.Mvvm)  |
| Cosmos DB        | Microsoft.Azure.Cosmos 3.x    |
| Blob Storage     | Azure.Storage.Blobs 12.x      |
| Table Storage    | Azure.Data.Tables 12.x        |
| Queue Storage    | Azure.Storage.Queues 12.x     |

## Security Notes

- Connection strings are stored in plain text in project files
- Do not commit project files containing sensitive connection strings to source control
- Consider using Azure Key Vault or environment variables for production scenarios

## License

MIT License
