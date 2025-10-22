using Newtonsoft.Json;

namespace CosmosDBIngester.Models;

public class FinancialDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonProperty("accountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    [JsonProperty("transactionType")]
    public string TransactionType { get; set; } = string.Empty;

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonProperty("merchantName")]
    public string MerchantName { get; set; } = string.Empty;

    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("sequenceNumber")]
    public long SequenceNumber { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;
}
