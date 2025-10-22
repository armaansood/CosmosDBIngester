# CosmosDBIngester

A GUI-based tool for bulk ingesting data into Azure Cosmos DB. This application allows you to test and benchmark your Cosmos DB account by ingesting large amounts of data with configurable parameters.

## Features

- **Easy Configuration**: Simple web interface to configure your Cosmos DB connection
- **Flexible Ingestion**: Configure batch size, document size, and workload patterns
- **Real-time Monitoring**: Live statistics showing documents ingested, data size, and throughput
- **Visual Charts**: Real-time chart showing data growth over time
- **Multiple Workload Types**:
  - **Sequential**: Creates documents with unique partition keys (best for distributed workloads)
  - **Random**: Uses random partition keys for each document
  - **Hot Partition**: All documents use the same partition key (useful for testing hot partition scenarios)

## Prerequisites

- .NET 8.0 SDK or later
- An Azure Cosmos DB account

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/armaansood/CosmosDBIngester.git
   cd CosmosDBIngester/CosmosDBIngester
   ```

2. Build the application:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Open your browser to `https://localhost:5001` (or the URL shown in the console)

## How to Use

### Step 1: Configure Connection Settings

1. **Cosmos DB Endpoint**: Enter your Cosmos DB endpoint URL (e.g., `https://your-account.documents.azure.com:443/`)
2. **Primary Key**: Enter your Cosmos DB primary key
3. **Database Name**: Enter the name of the database to use (will be created if it doesn't exist)
4. **Collection/Container Name**: Enter the container name (will be created if it doesn't exist)
5. **Throughput (RUs)**: Set the Request Units to provision if the container doesn't exist (minimum 400)
6. Click **Connect** to establish the connection

### Step 2: Configure Ingestion Settings

1. **Batch Size**: Number of documents to ingest per batch (1-1000)
2. **Document Size**: Size of each document in KB (1-100 KB)
3. **Workload Type**: Choose your workload pattern:
   - **Sequential**: Best for distributed load testing
   - **Random**: Simulates random data access patterns
   - **Hot Partition**: Tests single partition performance

### Step 3: Start Ingestion

1. Click **Start Ingestion** to begin
2. Monitor real-time statistics:
   - Total documents ingested
   - Total data size
   - Documents per second
   - Throughput (KB/s)
3. View the live chart showing data growth over time
4. Click **Stop Ingestion** when done

## Configuration

The application uses bulk execution mode for optimal performance. Key settings:
- Maximum retry attempts: 10
- Retry wait time: 30 seconds
- Statistics update interval: 1 second

## Architecture

- **Frontend**: Blazor Server with interactive components
- **Backend**: Azure Cosmos DB SDK v3 with bulk execution support
- **Data Generation**: Configurable document generation with padding to match specified size
- **Real-time Updates**: SignalR for live statistics updates

## Document Structure

Each ingested document contains:
- `id`: Unique identifier (GUID)
- `partitionKey`: Partition key (varies by workload type)
- `timestamp`: UTC timestamp of creation
- `workloadType`: The workload type used
- `data`: Padding data to reach target document size
- `sequenceNumber`: Sequential number for tracking

## Performance Tips

1. Start with small batch sizes (10-50) and increase gradually
2. Monitor RU consumption in the Azure Portal
3. Use Sequential workload for best overall throughput
4. For hot partition testing, ensure sufficient RUs are provisioned
5. Document size includes overhead (~200 bytes for metadata)

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
