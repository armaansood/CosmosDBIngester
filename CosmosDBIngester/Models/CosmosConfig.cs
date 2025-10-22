namespace CosmosDBIngester.Models;

public class CosmosConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string PrimaryKey { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public int ThroughputRUs { get; set; } = 400;
    public int BatchSize { get; set; } = 10;
    public int DocumentSizeKB { get; set; } = 1;
    public string WorkloadType { get; set; } = "Sequential";
    public DataType DataType { get; set; } = DataType.Financial;
}
