# Cosmos DB Bulk Data Ingester 🚀

A modern Windows Forms application for bulk ingesting realistic mock data into Azure Cosmos DB. Perfect for testing, demos, load testing, and development!

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows-blue?logo=windows)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Features

### 🎨 Modern UI
- **Dark Mode & Light Mode** - Toggle between themes
- **Resizable Window** - Maximize for better visibility
- **Real-time Statistics Dashboard** - Live ingestion metrics
- **Timestamped Status Log** - Track all operations

### 📊 Multiple Data Types
Generate realistic mock data using **Bogus**:
- 💰 **Financial** - Banking transactions, payments, accounts
- 🛒 **E-Commerce** - Orders, products, customers, shipping
- 🏥 **Healthcare** - Patient records, diagnoses, vitals, medications
- 🤖 **IoT** - Device telemetry, sensors, location data

### ⚡ Workload Patterns
- **Sequential** - Even partition distribution (balanced load)
- **Random** - Random partition assignment (varied access)
- **HotPartition** - Single partition (partition saturation testing)

### ⚙️ Flexible Configuration
- Batch Size: 1-1,000 documents
- Document Size: 1 KB - 2 MB (2,048 KB)
- Throughput: 400 - 1,000,000 RUs
- Change connections without restarting

## 🚀 Quick Start

### Download & Run (Windows)

1. **Download the latest release:**
   - Go to [Releases](https://github.com/armaansood/CosmosDBIngester/releases)
   - Download `CosmosDBIngester.zip`
   - Extract and run `CosmosDBIngester.exe`

2. **⚠️ Windows Security Warning:**
   
   When you first run the application, Windows SmartScreen may show:
   > "Windows protected your PC - Unknown publisher"
   
   **This is normal for unsigned applications.** To run:
   - Click **"More info"**
   - Click **"Run anyway"**
   
   **Or unblock the file:**
   ```powershell
   # Right-click → Properties → Check "Unblock" → Apply
   # Or use PowerShell:
   Unblock-File -Path ".\CosmosDBIngester.exe"
   ```
   
   **Why this happens:** The application is not digitally signed with a code signing certificate (~$200-500/year). See [CODE_SIGNING_GUIDE.md](CODE_SIGNING_GUIDE.md) for details.
   
   **Is it safe?** Yes! This is open-source software. You can review all code in this repository. The warning only means "we don't know the publisher" not "this is dangerous."

3. **Connect to Cosmos DB:**
   - Enter your Cosmos DB endpoint and primary key
   - Specify database and collection names
   - Click **Connect**

3. **Start Ingesting:**
   - Select data type and workload pattern
   - Configure batch size and document size
   - Click **Start Ingestion**

### Build from Source

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

## 📖 Documentation

For detailed usage instructions, see the [**User Guide**](USER_GUIDE.md).

## 📊 Screenshots

### Dark Mode
Beautiful dark theme for comfortable extended use with real-time statistics and status logging.

### Light Mode
Clean light theme with excellent readability and professional appearance.

## 💡 Use Cases

- **Load Testing** - Generate high-volume data to test Cosmos DB performance
- **Demo Data** - Populate databases with realistic sample data for demos
- **Development** - Create test datasets for application development
- **Benchmarking** - Test different partition strategies and configurations
- **Training** - Learn Cosmos DB concepts with hands-on data generation

## 🛠️ Technology Stack

- **Framework**: .NET 8.0 Windows Forms
- **Database SDK**: Microsoft.Azure.Cosmos v3.43.1
- **Data Generation**: Bogus v35.6.1
- **Features**: Bulk execution, dark/light themes, resizable UI

## 📦 Building a Release

Create a standalone executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output location: `CosmosDBIngester\bin\Release\net8.0-windows\win-x64\publish\`

## 🤝 Contributing

Contributions are welcome! Please feel free to:
- Report bugs via [Issues](https://github.com/armaansood/CosmosDBIngester/issues)
- Submit feature requests
- Create pull requests

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Azure Cosmos DB team for the excellent SDK
- Bogus library by Brian Chavez for realistic data generation
- Community contributors and testers

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/armaansood/CosmosDBIngester/issues)
- **Discussions**: [GitHub Discussions](https://github.com/armaansood/CosmosDBIngester/discussions)

---

**Made with ❤️ for the Cosmos DB community**

⭐ If you find this tool useful, please consider giving it a star!

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
