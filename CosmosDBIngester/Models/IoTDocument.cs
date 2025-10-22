using Newtonsoft.Json;

namespace CosmosDBIngester.Models;

public class IoTDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonProperty("deviceType")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonProperty("location")]
    public Location Location { get; set; } = new Location();

    [JsonProperty("temperature")]
    public double Temperature { get; set; }

    [JsonProperty("humidity")]
    public double Humidity { get; set; }

    [JsonProperty("pressure")]
    public double Pressure { get; set; }

    [JsonProperty("batteryLevel")]
    public int BatteryLevel { get; set; }

    [JsonProperty("signalStrength")]
    public int SignalStrength { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("firmware")]
    public string Firmware { get; set; } = string.Empty;

    [JsonProperty("sensorReadings")]
    public Dictionary<string, double> SensorReadings { get; set; } = new Dictionary<string, double>();

    [JsonProperty("sequenceNumber")]
    public long SequenceNumber { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;
}

public class Location
{
    [JsonProperty("latitude")]
    public double Latitude { get; set; }

    [JsonProperty("longitude")]
    public double Longitude { get; set; }

    [JsonProperty("city")]
    public string City { get; set; } = string.Empty;

    [JsonProperty("country")]
    public string Country { get; set; } = string.Empty;
}
