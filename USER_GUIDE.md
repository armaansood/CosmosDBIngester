# Cosmos DB Bulk Data Ingester - User Guide

## 📖 Overview
The Cosmos DB Bulk Data Ingester is a Windows Forms application that allows you to quickly populate Azure Cosmos DB with realistic mock data. Perfect for testing, demos, and development scenarios!

## ✨ Features

### 🎨 Modern UI
- **Dark Mode / Light Mode** - Toggle between themes with the ☀️/🌙 button
- **Resizable Window** - Maximize for better visibility of the status log
- **Real-time Statistics** - Monitor ingestion performance live
- **Status Log Console** - Track all operations with timestamps

### 📊 Data Types
Generate realistic mock data for four different domains:

1. **💰 Financial**
   - Banking transactions
   - Payment records
   - Account operations
   - Transaction IDs, amounts, currencies
   - Merchant information

2. **🛒 E-Commerce**
   - Customer orders
   - Product information
   - Shopping cart data
   - Shipping addresses
   - Payment methods

3. **🏥 Healthcare**
   - Patient records
   - Medical diagnoses
   - Treatment data
   - Vital signs
   - Medications

4. **🤖 IoT**
   - Device telemetry
   - Sensor readings
   - Location data
   - Battery and signal status
   - Environmental metrics

### ⚡ Workload Types
Test different partition strategies:

- **Sequential** - Even distribution across partitions (balanced load)
- **Random** - Randomly assigned partitions (simulates varied access patterns)
- **HotPartition** - All data to one partition (tests partition saturation)

### ⚙️ Configuration Options
- **Endpoint** - Your Cosmos DB endpoint URL
- **Primary Key** - Cosmos DB access key (hidden input for security)
- **Database Name** - Target database
- **Collection Name** - Target container
- **Throughput (RUs)** - Request Units provisioned (400-1,000,000)
- **Batch Size** - Documents per batch (1-1,000)
- **Document Size** - Size per document in KB (1-2,048 = up to 2MB)

## 🚀 Getting Started

### Prerequisites
- Windows operating system
- .NET 8.0 Runtime (or SDK if building from source)
- Azure Cosmos DB account

### Download & Run

#### Option 1: Download Pre-built Release (Recommended)
1. Go to the [Releases page](https://github.com/armaansood/CosmosDBIngester/releases)
2. Download the latest `CosmosDBIngester.zip`
3. Extract the ZIP file to a folder
4. Run `CosmosDBIngester.exe`

#### Option 2: Build from Source
```powershell
# Clone the repository
git clone https://github.com/armaansood/CosmosDBIngester.git
cd CosmosDBIngester

# Build the project
dotnet build -c Release

# Run the application
cd CosmosDBIngester\bin\Release\net8.0-windows
.\CosmosDBIngester.exe
```

## 📝 How to Use

### Step 1: Configure Connection
1. Enter your **Cosmos DB Endpoint** (e.g., `https://your-account.documents.azure.com:443/`)
2. Enter your **Primary Key** (masked for security)
3. Enter your **Database Name**
4. Enter your **Collection Name**
5. Set **Throughput (RUs)** if needed (default: 400)
6. Click **Connect**

### Step 2: Configure Ingestion Settings
1. Select **Data Type** from the dropdown
   - Hover over the field for descriptions of each type
2. Select **Workload Type**
   - Hover over the field for workload descriptions
3. Set **Batch Size** (how many documents to create per batch)
4. Set **Document Size (KB)** (1 KB to 2 MB per document)

### Step 3: Start Ingestion
1. Click **Start Ingestion** (blue button)
2. Watch real-time statistics update:
   - Total Documents
   - Total Data Size
   - Speed (docs/sec)
   - Throughput (MB/sec)
3. Monitor the **Status Log** for detailed operation info

### Step 4: Stop or Change Connection
- Click **Stop Ingestion** (red button) to halt the process
- Click **Change Connection** to switch to a different database/collection
- The app will safely stop any running ingestion before changing connections

### Step 5: Customize Appearance
- Click the **☀️ Light Mode** or **🌙 Dark Mode** button to toggle themes
- Maximize the window for a larger status log area

## 📊 Understanding Statistics

### Real-time Metrics
- **Total Documents**: Count of documents successfully ingested
- **Total Data Size**: Cumulative size of all data written (in MB)
- **Speed**: Documents per second ingestion rate
- **Throughput**: Megabytes per second data transfer rate

### Status Log
- Shows timestamped events
- Connection status updates
- Batch completion confirmations
- Error messages if any issues occur
- Auto-scrolls to show latest entries

## 💡 Tips & Best Practices

### Performance Optimization
1. **Start Small**: Begin with a batch size of 10-50 to test your configuration
2. **Monitor RUs**: Check Azure Portal for Request Unit consumption
3. **Scale Up**: Increase batch size gradually to find optimal performance
4. **Document Size**: Larger documents consume more RUs but test real-world scenarios

### Testing Scenarios
- **Load Testing**: Use Sequential workload with high batch size
- **Hot Partition Testing**: Use HotPartition workload to stress a single partition
- **Random Access**: Use Random workload to simulate varied access patterns
- **Large Documents**: Set document size to 1-2MB to test maximum payload scenarios

### Connection Management
- **Change Connection**: Use this feature to test multiple databases/collections without restarting
- **Connection Reuse**: The app maintains connections efficiently with bulk execution enabled
- **Secure Keys**: Primary keys are masked in the UI for security

## 🛠️ Troubleshooting

### Connection Issues
- **Problem**: "Failed to connect" error
- **Solution**: 
  - Verify endpoint URL format (should include `https://` and `:443/`)
  - Check primary key is correct
  - Ensure database and collection exist
  - Verify firewall rules allow your IP

### Performance Issues
- **Problem**: Slow ingestion rate
- **Solution**:
  - Increase provisioned throughput (RUs) in Azure Portal
  - Reduce batch size or document size
  - Check network connectivity
  - Avoid HotPartition workload for maximum throughput

### Button Disabled/Grayed Out
- **Problem**: Start, Stop, or Change Connection buttons are disabled
- **Solution**:
  - Connect to Cosmos DB first to enable Start button
  - Start ingestion to enable Stop button
  - Connect successfully to enable Change Connection button

## 🔧 Building a Release

To create a standalone executable:

```powershell
# Build in Release mode
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Output will be in:
# CosmosDBIngester\bin\Release\net8.0-windows\win-x64\publish\CosmosDBIngester.exe
```

## 📦 Technology Stack
- **.NET 8.0** - Windows Forms application framework
- **Microsoft.Azure.Cosmos SDK** - Cosmos DB client with bulk execution
- **Bogus** - Realistic fake data generation library
- **Windows Forms** - Modern dark/light mode UI

## 🤝 Contributing
Contributions are welcome! Please feel free to submit issues or pull requests.

## 📄 License
[Add your license information here]

## 🙏 Acknowledgments
- Cosmos DB team for the excellent SDK
- Bogus library for realistic data generation
- Community feedback and contributions

---

**Happy Testing! 🚀**

For issues or questions, please open an issue on the [GitHub repository](https://github.com/armaansood/CosmosDBIngester/issues).
