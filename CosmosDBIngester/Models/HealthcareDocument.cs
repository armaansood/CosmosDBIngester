using Newtonsoft.Json;

namespace CosmosDBIngester.Models;

public class HealthcareDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = string.Empty;

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonProperty("patientName")]
    public string PatientName { get; set; } = string.Empty;

    [JsonProperty("dateOfBirth")]
    public DateTime DateOfBirth { get; set; }

    [JsonProperty("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonProperty("diagnosisCode")]
    public string DiagnosisCode { get; set; } = string.Empty;

    [JsonProperty("diagnosis")]
    public string Diagnosis { get; set; } = string.Empty;

    [JsonProperty("treatmentType")]
    public string TreatmentType { get; set; } = string.Empty;

    [JsonProperty("physicianName")]
    public string PhysicianName { get; set; } = string.Empty;

    [JsonProperty("facilityName")]
    public string FacilityName { get; set; } = string.Empty;

    [JsonProperty("vitalSigns")]
    public VitalSigns VitalSigns { get; set; } = new VitalSigns();

    [JsonProperty("medications")]
    public List<string> Medications { get; set; } = new List<string>();

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("sequenceNumber")]
    public long SequenceNumber { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; } = string.Empty;
}

public class VitalSigns
{
    [JsonProperty("heartRate")]
    public int HeartRate { get; set; }

    [JsonProperty("bloodPressure")]
    public string BloodPressure { get; set; } = string.Empty;

    [JsonProperty("temperature")]
    public double Temperature { get; set; }

    [JsonProperty("oxygenSaturation")]
    public int OxygenSaturation { get; set; }
}
