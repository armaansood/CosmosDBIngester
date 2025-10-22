using Newtonsoft.Json;

namespace CosmosDBIngester.Models;

public class SampleDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty("workloadType")]
    public string WorkloadType { get; set; } = string.Empty;

    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;

    [JsonProperty("sequenceNumber")]
    public long SequenceNumber { get; set; }
}
