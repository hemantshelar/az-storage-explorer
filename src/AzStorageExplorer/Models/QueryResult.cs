namespace AzStorageExplorer.Models;

public class CosmosQueryResult
{
    public List<dynamic> Results { get; set; } = new();
    public double RequestCharge { get; set; }
    public string? ContinuationToken { get; set; }
}

