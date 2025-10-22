namespace CosmosDBIngester.Models;

public class IngestionStats
{
    public DateTime Timestamp { get; set; }
    public long TotalDocuments { get; set; }
    public long TotalDataSizeKB { get; set; }
    public double DocumentsPerSecond { get; set; }
    public double KBPerSecond { get; set; }
}
