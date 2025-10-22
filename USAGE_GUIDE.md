# Cosmos DB Ingester - Usage Guide

## Overview
This is a console application (.exe) that ingests bulk data into Azure Cosmos DB with support for multiple data types and realistic mock data generation using the Bogus library.

## Running the Application

### Windows
Navigate to the build output directory and run:
```
.\CosmosDBIngester.exe
```

Or from the project directory:
```
dotnet run --project CosmosDBIngester\CosmosDBIngester.csproj
```

### Build
```
dotnet build CosmosDBIngester\CosmosDBIngester.csproj
```

## Features

### Data Types
The application supports four different data types with realistic mock data:

1. **Financial** - Banking transactions, payments, and financial records
   - Transaction IDs, account numbers
   - Transaction types (Debit, Credit, Transfer, etc.)
   - Amounts, currency codes
   - Merchant names and categories
   - Transaction status

2. **E-Commerce** - Online shopping orders and customer data
   - Order IDs and customer information
   - Product names and categories
   - Quantities, prices, and totals
   - Shipping addresses
   - Order status and payment methods

3. **Healthcare** - Patient records and medical data
   - Patient IDs and demographics
   - Diagnoses and treatment types
   - Physician and facility information
   - Vital signs (heart rate, blood pressure, temperature, oxygen saturation)
   - Medications
   - Patient status

4. **IoT** - Internet of Things device telemetry
   - Device IDs and types
   - Location data (latitude, longitude, city, country)
   - Sensor readings (temperature, humidity, pressure)
   - Battery level and signal strength
   - Device status and firmware version
   - Custom sensor readings

### Workload Types
- **Sequential**: Even distribution across partitions
- **Random**: Random partition assignment
- **HotPartition**: All documents to a single partition (for testing hot partition scenarios)

### Configuration Options
- **Endpoint**: Your Cosmos DB endpoint URL
- **Primary Key**: Cosmos DB access key (securely entered with masked input)
- **Database Name**: Target database name
- **Collection Name**: Target container/collection name
- **Throughput (RUs)**: Request Units (default: 400)
- **Batch Size**: Number of documents per batch (default: 10)
- **Document Size (KB)**: Size of each document in kilobytes (default: 1)

## Menu Options

1. **Configure Data Ingestion Settings** - Set data type, workload type, batch size, and document size
2. **Start Data Ingestion** - Begin ingesting data with current settings
3. **Stop Data Ingestion** - Stop the current ingestion process
4. **Exit** - Close the application

## Real-time Statistics
During ingestion, the application displays:
- Total documents ingested
- Total data size (MB)
- Documents per second
- MB per second throughput

## Tips
- Start with small batch sizes to test your configuration
- Monitor RU consumption in Azure Portal
- Use Hot Partition mode to test partition saturation
- The application uses bulk execution for optimal performance
- Press 'S' during ingestion to stop gracefully

## Example Session
```
1. Launch the application
2. Enter your Cosmos DB credentials
3. Select option 1 to configure settings
4. Choose data type (e.g., Financial)
5. Choose workload type (e.g., Sequential)
6. Set batch size and document size
7. Select option 2 to start ingestion
8. Monitor real-time statistics
9. Press 'S' to stop when desired
```

## Technology Stack
- .NET 8.0
- Microsoft.Azure.Cosmos SDK
- Bogus (for realistic mock data generation)
