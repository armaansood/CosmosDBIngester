using Microsoft.Azure.Cosmos;
using CosmosDBIngester.Models;
using CosmosDBIngester.Security;
using System.Diagnostics;
using Bogus;
using CosmosDatabase = Microsoft.Azure.Cosmos.Database;
using DocumentDataType = CosmosDBIngester.Models.DataType;

namespace CosmosDBIngester.Services;

public class CosmosDbService : IDisposable
{
    private CosmosClient? _client;
    private Container? _container;
    private CosmosDatabase? _database;
    private long _documentCounter = 0;
    private long _totalDataSizeKB = 0;
    private DateTime _startTime;
    private volatile bool _isRunning = false;
    private bool _disposed = false;
    private readonly object _statsLock = new object();
    private readonly AuditLogger _auditLogger = new AuditLogger();

    // Reusable Faker instances (thread-safe)
    private static readonly Faker _faker = new Faker();

    public event Action<IngestionStats>? OnStatsUpdated;
    public event Action<string>? OnStatusChanged;

    public bool IsRunning => _isRunning;

    public async Task<bool> InitializeAsync(CosmosConfig config)
    {
        try
        {
            // Security: Comprehensive input validation
            var endpointResult = InputValidator.ValidateEndpoint(config.Endpoint);
            if (!endpointResult.IsValid)
            {
                _auditLogger.LogAuthenticationAttempt(config.Endpoint, false, "Invalid endpoint format");
                throw new ArgumentException(endpointResult.ErrorMessage, nameof(config.Endpoint));
            }
            
            var primaryKey = config.GetPrimaryKey();
            var keyResult = InputValidator.ValidatePrimaryKey(primaryKey);
            if (!keyResult.IsValid)
            {
                _auditLogger.LogAuthenticationAttempt(config.Endpoint, false, "Invalid primary key format");
                throw new ArgumentException(keyResult.ErrorMessage, nameof(config));
            }
            
            var dbResult = InputValidator.ValidateDatabaseName(config.DatabaseName);
            if (!dbResult.IsValid)
            {
                _auditLogger.LogSecurityEvent("InvalidDatabaseName", dbResult.ErrorMessage, "Warning");
                throw new ArgumentException(dbResult.ErrorMessage, nameof(config.DatabaseName));
            }
            
            var collResult = InputValidator.ValidateCollectionName(config.CollectionName);
            if (!collResult.IsValid)
            {
                _auditLogger.LogSecurityEvent("InvalidCollectionName", collResult.ErrorMessage, "Warning");
                throw new ArgumentException(collResult.ErrorMessage, nameof(config.CollectionName));
            }
            
            var throughputResult = InputValidator.ValidateThroughput(config.ThroughputRUs);
            if (!throughputResult.IsValid)
            {
                throw new ArgumentException(throughputResult.ErrorMessage, nameof(config.ThroughputRUs));
            }

            OnStatusChanged?.Invoke("Initializing secure connection to Cosmos DB...");

            // Security: TLS 1.2+ enforcement (handled by CosmosClient)
            var options = new CosmosClientOptions
            {
                AllowBulkExecution = true,
                MaxRetryAttemptsOnRateLimitedRequests = 10,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                RequestTimeout = TimeSpan.FromSeconds(60)
            };

            _client = new CosmosClient(config.Endpoint, primaryKey, options);

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
            _auditLogger.LogAuthenticationAttempt(config.Endpoint, true);
            return true;
        }
        catch (ArgumentException ex)
        {
            _auditLogger.LogException("InitializeAsync", ex);
            OnStatusChanged?.Invoke(SecureErrorHandler.GetUserFriendlyMessage(ex));
            return false;
        }
        catch (CosmosException ex)
        {
            _auditLogger.LogException("InitializeAsync", ex);
            if (SecureErrorHandler.IsSecuritySensitive(ex))
            {
                _auditLogger.LogSecurityEvent("AuthenticationFailure", ex.Message, "Critical");
            }
            OnStatusChanged?.Invoke(SecureErrorHandler.GetUserFriendlyMessage(ex));
            return false;
        }
        catch (Exception ex)
        {
            _auditLogger.LogException("InitializeAsync", ex);
            OnStatusChanged?.Invoke(SecureErrorHandler.GetUserFriendlyMessage(ex));
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
        Interlocked.Exchange(ref _documentCounter, 0);
        Interlocked.Exchange(ref _totalDataSizeKB, 0);
        _startTime = DateTime.UtcNow;

        // Security: Audit log ingestion start
        _auditLogger.LogIngestionStart(
            config.DataType.ToString(),
            config.WorkloadType,
            config.BatchSize,
            config.DocumentSizeKB
        );

        OnStatusChanged?.Invoke("Starting data ingestion...");

        try
        {
            var lastStatsUpdate = DateTime.UtcNow;
            var statsUpdateInterval = TimeSpan.FromSeconds(1);

            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                // Security: Check cancellation token before expensive operations
                cancellationToken.ThrowIfCancellationRequested();
                
                var batch = GenerateBatch(config);
                await IngestBatchAsync(batch, cancellationToken);

                Interlocked.Add(ref _documentCounter, batch.Count);
                Interlocked.Add(ref _totalDataSizeKB, batch.Count * config.DocumentSizeKB);

                if (DateTime.UtcNow - lastStatsUpdate >= statsUpdateInterval)
                {
                    var elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
                    var docCount = Interlocked.Read(ref _documentCounter);
                    var dataSize = Interlocked.Read(ref _totalDataSizeKB);
                    
                    var stats = new IngestionStats
                    {
                        Timestamp = DateTime.UtcNow,
                        TotalDocuments = docCount,
                        TotalDataSizeKB = dataSize,
                        DocumentsPerSecond = elapsed > 0 ? docCount / elapsed : 0,
                        KBPerSecond = elapsed > 0 ? dataSize / elapsed : 0
                    };

                    OnStatsUpdated?.Invoke(stats);
                    lastStatsUpdate = DateTime.UtcNow;
                }
            }

            // Security: Audit log ingestion stop
            var duration = (DateTime.UtcNow - _startTime).TotalSeconds;
            _auditLogger.LogIngestionStop(
                Interlocked.Read(ref _documentCounter),
                Interlocked.Read(ref _totalDataSizeKB),
                duration
            );

            OnStatusChanged?.Invoke("Ingestion stopped.");
        }
        catch (OperationCanceledException)
        {
            var duration = (DateTime.UtcNow - _startTime).TotalSeconds;
            _auditLogger.LogIngestionStop(
                Interlocked.Read(ref _documentCounter),
                Interlocked.Read(ref _totalDataSizeKB),
                duration
            );
            OnStatusChanged?.Invoke("Ingestion cancelled by user.");
        }
        catch (Exception ex)
        {
            _auditLogger.LogException("StartIngestionAsync", ex);
            OnStatusChanged?.Invoke(SecureErrorHandler.GetUserFriendlyMessage(ex));
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
        var batch = new List<object>(config.BatchSize);
        var dataSize = config.DocumentSizeKB * 1024;
        // Reduced overhead estimate for more accurate padding
        var paddingData = new string('X', Math.Max(0, dataSize - 500));

        var currentCounter = Interlocked.Read(ref _documentCounter);

        for (int i = 0; i < config.BatchSize; i++)
        {
            var partitionKey = config.WorkloadType switch
            {
                "Random" => Guid.NewGuid().ToString(),
                "HotPartition" => "hot-partition-1",
                _ => $"partition-{currentCounter + i}"
            };

            object doc = config.DataType switch
            {
                DocumentDataType.Financial => GenerateFinancialDocument(partitionKey, paddingData, currentCounter + i),
                DocumentDataType.ECommerce => GenerateECommerceDocument(partitionKey, paddingData, currentCounter + i),
                DocumentDataType.Healthcare => GenerateHealthcareDocument(partitionKey, paddingData, currentCounter + i),
                DocumentDataType.IoT => GenerateIoTDocument(partitionKey, paddingData, currentCounter + i),
                _ => GenerateFinancialDocument(partitionKey, paddingData, currentCounter + i)
            };

            batch.Add(doc);
        }

        return batch;
    }

    private FinancialDocument GenerateFinancialDocument(string partitionKey, string paddingData, long sequenceNumber)
    {
        return new FinancialDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = partitionKey,
            Timestamp = DateTime.UtcNow,
            TransactionId = $"TXN-{_faker.Random.AlphaNumeric(12).ToUpper()}",
            AccountNumber = _faker.Finance.Account(),
            TransactionType = _faker.PickRandom(new[] { "Debit", "Credit", "Transfer", "Withdrawal", "Deposit" }),
            Amount = _faker.Finance.Amount(10, 10000),
            Currency = _faker.Finance.Currency().Code,
            MerchantName = _faker.Company.CompanyName(),
            Category = _faker.PickRandom(new[] { "Groceries", "Entertainment", "Travel", "Healthcare", "Utilities", "Shopping" }),
            Status = _faker.PickRandom(new[] { "Completed", "Pending", "Failed", "Processing" }),
            Description = _faker.Lorem.Sentence(),
            SequenceNumber = sequenceNumber,
            Data = paddingData
        };
    }

    private ECommerceDocument GenerateECommerceDocument(string partitionKey, string paddingData, long sequenceNumber)
    {
        var quantity = _faker.Random.Int(1, 10);
        var price = decimal.Parse(_faker.Commerce.Price(10, 1000));
        
        return new ECommerceDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = partitionKey,
            Timestamp = DateTime.UtcNow,
            OrderId = $"ORD-{_faker.Random.AlphaNumeric(10).ToUpper()}",
            CustomerId = $"CUST-{_faker.Random.AlphaNumeric(8).ToUpper()}",
            CustomerName = _faker.Name.FullName(),
            Email = _faker.Internet.Email(),
            ProductName = _faker.Commerce.ProductName(),
            ProductCategory = _faker.Commerce.Department(),
            Quantity = quantity,
            Price = price,
            TotalAmount = price * quantity,
            ShippingAddress = _faker.Address.FullAddress(),
            OrderStatus = _faker.PickRandom(new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" }),
            PaymentMethod = _faker.PickRandom(new[] { "Credit Card", "Debit Card", "PayPal", "Bank Transfer" }),
            SequenceNumber = sequenceNumber,
            Data = paddingData
        };
    }

    private HealthcareDocument GenerateHealthcareDocument(string partitionKey, string paddingData, long sequenceNumber)
    {
        return new HealthcareDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = partitionKey,
            Timestamp = DateTime.UtcNow,
            PatientId = $"PAT-{_faker.Random.AlphaNumeric(8).ToUpper()}",
            PatientName = _faker.Name.FullName(),
            DateOfBirth = _faker.Date.Past(70, DateTime.Now.AddYears(-18)),
            Gender = _faker.PickRandom(new[] { "Male", "Female", "Other" }),
            DiagnosisCode = $"ICD-{_faker.Random.AlphaNumeric(5).ToUpper()}",
            Diagnosis = _faker.PickRandom(new[] { "Hypertension", "Diabetes Type 2", "Asthma", "Arthritis", "Migraine", "Allergy" }),
            TreatmentType = _faker.PickRandom(new[] { "Medication", "Surgery", "Therapy", "Observation", "Vaccination" }),
            PhysicianName = $"Dr. {_faker.Name.FullName()}",
            FacilityName = $"{_faker.Address.City()} Medical Center",
            VitalSigns = new VitalSigns
            {
                HeartRate = _faker.Random.Int(60, 100),
                BloodPressure = $"{_faker.Random.Int(110, 140)}/{_faker.Random.Int(70, 90)}",
                Temperature = Math.Round(_faker.Random.Double(97.0, 99.5), 1),
                OxygenSaturation = _faker.Random.Int(95, 100)
            },
            Medications = _faker.Make(_faker.Random.Int(1, 4), () => _faker.Commerce.ProductName()).ToList(),
            Status = _faker.PickRandom(new[] { "Active", "Discharged", "Under Observation", "Critical" }),
            SequenceNumber = sequenceNumber,
            Data = paddingData
        };
    }

    private IoTDocument GenerateIoTDocument(string partitionKey, string paddingData, long sequenceNumber)
    {
        return new IoTDocument
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = partitionKey,
            Timestamp = DateTime.UtcNow,
            DeviceId = $"IOT-{_faker.Random.AlphaNumeric(12).ToUpper()}",
            DeviceType = _faker.PickRandom(new[] { "Temperature Sensor", "Humidity Sensor", "Motion Detector", "Smart Meter", "Weather Station" }),
            Location = new Location
            {
                Latitude = _faker.Address.Latitude(),
                Longitude = _faker.Address.Longitude(),
                City = _faker.Address.City(),
                Country = _faker.Address.Country()
            },
            Temperature = Math.Round(_faker.Random.Double(15.0, 35.0), 2),
            Humidity = Math.Round(_faker.Random.Double(30.0, 90.0), 2),
            Pressure = Math.Round(_faker.Random.Double(980.0, 1050.0), 2),
            BatteryLevel = _faker.Random.Int(0, 100),
            SignalStrength = _faker.Random.Int(-100, -30),
            Status = _faker.PickRandom(new[] { "Online", "Offline", "Maintenance", "Error" }),
            Firmware = $"v{_faker.Random.Int(1, 3)}.{_faker.Random.Int(0, 9)}.{_faker.Random.Int(0, 99)}",
            SensorReadings = new Dictionary<string, double>
            {
                { "vibration", Math.Round(_faker.Random.Double(0.0, 10.0), 2) },
                { "noise", Math.Round(_faker.Random.Double(30.0, 100.0), 2) },
                { "light", Math.Round(_faker.Random.Double(0.0, 1000.0), 2) }
            },
            SequenceNumber = sequenceNumber,
            Data = paddingData
        };
    }

    private async Task IngestBatchAsync(List<object> documents, CancellationToken cancellationToken)
    {
        if (_container == null) return;

        var tasks = new List<Task>(documents.Count);

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

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // Log individual task failures
            var failedTasks = tasks.Where(t => t.IsFaulted).ToList();
            if (failedTasks.Any())
            {
                OnStatusChanged?.Invoke($"Warning: {failedTasks.Count} documents failed to ingest. First error: {failedTasks[0].Exception?.GetBaseException().Message}");
            }
            
            // Re-throw if it's a cancellation or critical error
            if (ex is OperationCanceledException)
                throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            StopIngestion();
            _client?.Dispose();
            _client = null;
            _container = null;
            _database = null;
            _auditLogger?.Dispose();
        }

        _disposed = true;
    }

    ~CosmosDbService()
    {
        Dispose(false);
    }
}
