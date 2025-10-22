using Microsoft.Azure.Cosmos;
using CosmosDBIngester.Models;
using System.Diagnostics;

namespace CosmosDBIngester.Services;

public class CosmosDbService
{
    private CosmosClient? _client;
    private Container? _container;
    private Database? _database;
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

    private List<SampleDocument> GenerateBatch(CosmosConfig config)
    {
        var batch = new List<SampleDocument>();
        var dataSize = config.DocumentSizeKB * 1024;
        var paddingData = new string('X', Math.Max(0, dataSize - 200));

        for (int i = 0; i < config.BatchSize; i++)
        {
            var doc = new SampleDocument
            {
                Id = Guid.NewGuid().ToString(),
                PartitionKey = config.WorkloadType switch
                {
                    "Random" => Guid.NewGuid().ToString(),
                    "HotPartition" => "hot-partition-1",
                    _ => $"partition-{_documentCounter + i}"
                },
                Timestamp = DateTime.UtcNow,
                WorkloadType = config.WorkloadType,
                Data = paddingData,
                SequenceNumber = _documentCounter + i
            };

            batch.Add(doc);
        }

        return batch;
    }

    private async Task IngestBatchAsync(List<SampleDocument> documents, CancellationToken cancellationToken)
    {
        if (_container == null) return;

        var tasks = new List<Task>();

        foreach (var doc in documents)
        {
            tasks.Add(_container.CreateItemAsync(doc, new PartitionKey(doc.PartitionKey), cancellationToken: cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
