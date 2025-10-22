using Microsoft.Azure.Cosmos;
using CosmosDBIngester.Models;
using System.Diagnostics;
using Bogus;
using CosmosDatabase = Microsoft.Azure.Cosmos.Database;
using DocumentDataType = CosmosDBIngester.Models.DataType;

namespace CosmosDBIngester.Services;

public class CosmosDbService
{
    private CosmosClient? _client;
    private Container? _container;
    private CosmosDatabase? _database;
    private long _documentCounter = 0;
    private long _totalDataSizeKB = 0;
    private DateTime _startTime;
    private bool _isRunning = false;

    public event Action<IngestionStats>? OnStatsUpdated;
    public event Action<string>? OnStatusChanged;

    public bool IsRunning => _isRunning;

    public async Task<bool> InitializeAsync(CosmosConfig config)
    {
        try
        {
            OnStatusChanged?.Invoke("Initializing connection to Cosmos DB...");

            var options = new CosmosClientOptions
            {
                AllowBulkExecution = true,
                MaxRetryAttemptsOnRateLimitedRequests = 10,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30)
            };

            _client = new CosmosClient(config.Endpoint, config.PrimaryKey, options);

            OnStatusChanged?.Invoke("Creating or getting database...");
            _database = await _client.CreateDatabaseIfNotExistsAsync(config.DatabaseName);

            OnStatusChanged?.Invoke("Creating or getting container...");
            var containerProperties = new ContainerProperties
            {
                Id = config.CollectionName,
                PartitionKeyPath = "/partitionKey"
            };

            _container = await _database.CreateContainerIfNotExistsAsync(
                containerProperties,
                config.ThroughputRUs
            );

            OnStatusChanged?.Invoke("Connection established successfully!");
            return true;
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"Error: {ex.Message}");
            return false;
        }
    }

    public async Task StartIngestionAsync(CosmosConfig config, CancellationToken cancellationToken)
    {
        if (_container == null)
        {
            OnStatusChanged?.Invoke("Please initialize connection first!");
            return;
        }

        _isRunning = true;
        _documentCounter = 0;
        _totalDataSizeKB = 0;
        _startTime = DateTime.UtcNow;

        OnStatusChanged?.Invoke("Starting data ingestion...");

        try
        {
            var lastStatsUpdate = DateTime.UtcNow;
            var statsUpdateInterval = TimeSpan.FromSeconds(1);

            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                var batch = GenerateBatch(config);
                await IngestBatchAsync(batch, cancellationToken);

                _documentCounter += batch.Count;
                _totalDataSizeKB += batch.Count * config.DocumentSizeKB;

                if (DateTime.UtcNow - lastStatsUpdate >= statsUpdateInterval)
                {
                    var elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
                    var stats = new IngestionStats
                    {
                        Timestamp = DateTime.UtcNow,
                        TotalDocuments = _documentCounter,
                        TotalDataSizeKB = _totalDataSizeKB,
                        DocumentsPerSecond = _documentCounter / elapsed,
                        KBPerSecond = _totalDataSizeKB / elapsed
                    };

                    OnStatsUpdated?.Invoke(stats);
                    lastStatsUpdate = DateTime.UtcNow;
                }
            }

            OnStatusChanged?.Invoke("Ingestion stopped.");
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"Error during ingestion: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void StopIngestion()
    {
        _isRunning = false;
        OnStatusChanged?.Invoke("Stopping ingestion...");
    }

    private List<object> GenerateBatch(CosmosConfig config)
    {
        var batch = new List<object>();
        var dataSize = config.DocumentSizeKB * 1024;
        var paddingData = new string('X', Math.Max(0, dataSize - 500));

        for (int i = 0; i < config.BatchSize; i++)
        {
            var partitionKey = config.WorkloadType switch
            {
                "Random" => Guid.NewGuid().ToString(),
                "HotPartition" => "hot-partition-1",
                _ => $"partition-{_documentCounter + i}"
            };

            object doc = config.DataType switch
            {
                DocumentDataType.Financial => GenerateFinancialDocument(partitionKey, paddingData, _documentCounter + i),
                DocumentDataType.ECommerce => GenerateECommerceDocument(partitionKey, paddingData, _documentCounter + i),
                DocumentDataType.Healthcare => GenerateHealthcareDocument(partitionKey, paddingData, _documentCounter + i),
                DocumentDataType.IoT => GenerateIoTDocument(partitionKey, paddingData, _documentCounter + i),
                _ => GenerateFinancialDocument(partitionKey, paddingData, _documentCounter + i)
            };

            batch.Add(doc);
        }

        return batch;
    }

    private FinancialDocument GenerateFinancialDocument(string partitionKey, string paddingData, long sequenceNumber)
    {
        var faker = new Faker();
        return new FinancialDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = partitionKey,
            Timestamp = DateTime.UtcNow,
            TransactionId = $"TXN-{faker.Random.AlphaNumeric(12).ToUpper()}",
            AccountNumber = faker.Finance.Account(),
            TransactionType = faker.PickRandom(new[] { "Debit", "Credit", "Transfer", "Withdrawal", "Deposit" }),
            Amount = faker.Finance.Amount(10, 10000),
            Currency = faker.Finance.Currency().Code,
            MerchantName = faker.Company.CompanyName(),
            Category = faker.PickRandom(new[] { "Groceries", "Entertainment", "Travel", "Healthcare", "Utilities", "Shopping" }),
            Status = faker.PickRandom(new[] { "Completed", "Pending", "Failed", "Processing" }),
            Description = faker.Lorem.Sentence(),
            SequenceNumber = sequenceNumber,
            Data = paddingData
        };
    }

    private ECommerceDocument GenerateECommerceDocument(string partitionKey, string paddingData, long sequenceNumber)
    {
        var faker = new Faker();
        var quantity = faker.Random.Int(1, 10);
        var price = decimal.Parse(faker.Commerce.Price(10, 1000));
        
        return new ECommerceDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = partitionKey,
            Timestamp = DateTime.UtcNow,
            OrderId = $"ORD-{faker.Random.AlphaNumeric(10).ToUpper()}",
            CustomerId = $"CUST-{faker.Random.AlphaNumeric(8).ToUpper()}",
            CustomerName = faker.Name.FullName(),
            Email = faker.Internet.Email(),
            ProductName = faker.Commerce.ProductName(),
            ProductCategory = faker.Commerce.Department(),
            Quantity = quantity,
            Price = price,
            TotalAmount = price * quantity,
            ShippingAddress = faker.Address.FullAddress(),
            OrderStatus = faker.PickRandom(new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" }),
            PaymentMethod = faker.PickRandom(new[] { "Credit Card", "Debit Card", "PayPal", "Bank Transfer" }),
            SequenceNumber = sequenceNumber,
            Data = paddingData
        };
    }

    private HealthcareDocument GenerateHealthcareDocument(string partitionKey, string paddingData, long sequenceNumber)
    {
        var faker = new Faker();
        
        return new HealthcareDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = partitionKey,
            Timestamp = DateTime.UtcNow,
            PatientId = $"PAT-{faker.Random.AlphaNumeric(8).ToUpper()}",
            PatientName = faker.Name.FullName(),
            DateOfBirth = faker.Date.Past(70, DateTime.Now.AddYears(-18)),
            Gender = faker.PickRandom(new[] { "Male", "Female", "Other" }),
            DiagnosisCode = $"ICD-{faker.Random.AlphaNumeric(5).ToUpper()}",
            Diagnosis = faker.PickRandom(new[] { "Hypertension", "Diabetes Type 2", "Asthma", "Arthritis", "Migraine", "Allergy" }),
            TreatmentType = faker.PickRandom(new[] { "Medication", "Surgery", "Therapy", "Observation", "Vaccination" }),
            PhysicianName = $"Dr. {faker.Name.FullName()}",
            FacilityName = $"{faker.Address.City()} Medical Center",
            VitalSigns = new VitalSigns
            {
                HeartRate = faker.Random.Int(60, 100),
                BloodPressure = $"{faker.Random.Int(110, 140)}/{faker.Random.Int(70, 90)}",
                Temperature = Math.Round(faker.Random.Double(97.0, 99.5), 1),
                OxygenSaturation = faker.Random.Int(95, 100)
            },
            Medications = faker.Make(faker.Random.Int(1, 4), () => faker.Commerce.ProductName()).ToList(),
            Status = faker.PickRandom(new[] { "Active", "Discharged", "Under Observation", "Critical" }),
            SequenceNumber = sequenceNumber,
            Data = paddingData
        };
    }

    private IoTDocument GenerateIoTDocument(string partitionKey, string paddingData, long sequenceNumber)
    {
        var faker = new Faker();
        
        return new IoTDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = partitionKey,
            Timestamp = DateTime.UtcNow,
            DeviceId = $"IOT-{faker.Random.AlphaNumeric(12).ToUpper()}",
            DeviceType = faker.PickRandom(new[] { "Temperature Sensor", "Humidity Sensor", "Motion Detector", "Smart Meter", "Weather Station" }),
            Location = new Location
            {
                Latitude = faker.Address.Latitude(),
                Longitude = faker.Address.Longitude(),
                City = faker.Address.City(),
                Country = faker.Address.Country()
            },
            Temperature = Math.Round(faker.Random.Double(15.0, 35.0), 2),
            Humidity = Math.Round(faker.Random.Double(30.0, 90.0), 2),
            Pressure = Math.Round(faker.Random.Double(980.0, 1050.0), 2),
            BatteryLevel = faker.Random.Int(0, 100),
            SignalStrength = faker.Random.Int(-100, -30),
            Status = faker.PickRandom(new[] { "Online", "Offline", "Maintenance", "Error" }),
            Firmware = $"v{faker.Random.Int(1, 3)}.{faker.Random.Int(0, 9)}.{faker.Random.Int(0, 99)}",
            SensorReadings = new Dictionary<string, double>
            {
                { "vibration", Math.Round(faker.Random.Double(0.0, 10.0), 2) },
                { "noise", Math.Round(faker.Random.Double(30.0, 100.0), 2) },
                { "light", Math.Round(faker.Random.Double(0.0, 1000.0), 2) }
            },
            SequenceNumber = sequenceNumber,
            Data = paddingData
        };
    }

    private async Task IngestBatchAsync(List<object> documents, CancellationToken cancellationToken)
    {
        if (_container == null) return;

        var tasks = new List<Task>();

        foreach (var doc in documents)
        {
            string partitionKey = doc switch
            {
                FinancialDocument fd => fd.PartitionKey,
                ECommerceDocument ed => ed.PartitionKey,
                HealthcareDocument hd => hd.PartitionKey,
                IoTDocument id => id.PartitionKey,
                _ => throw new InvalidOperationException("Unknown document type")
            };

            tasks.Add(_container.CreateItemAsync(doc, new PartitionKey(partitionKey), cancellationToken: cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
