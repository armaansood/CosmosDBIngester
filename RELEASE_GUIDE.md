# Creating a GitHub Release for Cosmos DB Ingester

## Step 1: Build the Release

Run this command in PowerShell from the project root directory:

```powershell
# Navigate to project root
cd d:\CosmosDBIngester

# Build release version
dotnet publish CosmosDBIngester\CosmosDBIngester.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false

# Alternative: Self-contained single file (larger but no runtime needed)
dotnet publish CosmosDBIngester\CosmosDBIngester.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output will be in:
- Standard: `CosmosDBIngester\bin\Release\net8.0-windows\win-x64\publish\`
- Single File: `CosmosDBIngester\bin\Release\net8.0-windows\win-x64\publish\`

## Step 2: Prepare Release Files

### For Framework-Dependent Release (Recommended)
Users need .NET 8.0 Runtime installed, but smaller download size.

```powershell
cd CosmosDBIngester\bin\Release\net8.0-windows\win-x64\publish
Compress-Archive -Path * -DestinationPath ..\..\..\..\..\..\CosmosDBIngester-v1.0.0.zip
```

### For Self-Contained Release
Larger file but includes .NET runtime - no prerequisites needed.

```powershell
cd CosmosDBIngester\bin\Release\net8.0-windows\win-x64\publish
Compress-Archive -Path * -DestinationPath ..\..\..\..\..\..\CosmosDBIngester-v1.0.0-standalone.zip
```

## Step 3: Create GitHub Release

1. Go to your repository: https://github.com/armaansood/CosmosDBIngester

2. Click on **Releases** (right sidebar)

3. Click **Draft a new release**

4. Fill in the release information:

### Tag Version
```
v1.0.0
```

### Release Title
```
Cosmos DB Bulk Data Ingester v1.0.0
```

### Description
```markdown
## 🚀 Cosmos DB Bulk Data Ingester v1.0.0

A modern Windows Forms application for bulk ingesting realistic mock data into Azure Cosmos DB.

### ✨ Features

- 🎨 **Dark Mode & Light Mode** - Beautiful, modern UI with theme toggle
- 📊 **4 Data Types** - Financial, E-Commerce, Healthcare, IoT with realistic mock data
- ⚡ **3 Workload Patterns** - Sequential, Random, and Hot Partition testing
- 📈 **Real-time Statistics** - Live ingestion metrics and throughput monitoring
- 🔄 **Connection Management** - Change connections without restarting
- 📏 **Flexible Configuration** - Batch sizes 1-1000, document sizes 1KB-2MB
- 🪟 **Resizable Window** - Maximize for better visibility

### 📥 Download

Choose the version that's right for you:

#### Framework-Dependent (Recommended)
**Requirements:** .NET 8.0 Runtime must be installed
- **Size:** ~500 KB
- **Download:** [CosmosDBIngester-v1.0.0.zip](link-to-zip)
- **Get .NET 8.0 Runtime:** https://dotnet.microsoft.com/download/dotnet/8.0/runtime

#### Self-Contained Standalone
**Requirements:** None - includes .NET runtime
- **Size:** ~60 MB
- **Download:** [CosmosDBIngester-v1.0.0-standalone.zip](link-to-zip)

### 🚀 Quick Start

1. Download and extract the ZIP file
2. Run `CosmosDBIngester.exe`
3. Enter your Cosmos DB connection details
4. Select data type and workload pattern
5. Click **Start Ingestion**

### 📖 Documentation

- [User Guide](https://github.com/armaansood/CosmosDBIngester/blob/main/USER_GUIDE.md)
- [Usage Guide](https://github.com/armaansood/CosmosDBIngester/blob/main/USAGE_GUIDE.md)

### 🛠️ Technical Details

- **Framework:** .NET 8.0 Windows Forms
- **Database SDK:** Microsoft.Azure.Cosmos v3.43.1
- **Data Generation:** Bogus v35.6.1
- **Platform:** Windows (x64)

### 📝 Changelog

#### New Features
- ✨ Modern Windows Forms UI with dark/light mode
- ✨ 4 realistic data types using Bogus library
- ✨ Connection change functionality
- ✨ Resizable and maximizable window
- ✨ Document size up to 2MB
- ✨ Tooltips with descriptions

#### Improvements
- 🎨 Beautiful dark mode interface
- 📊 Real-time statistics dashboard
- 🔍 Timestamped status log console
- ⚙️ Enhanced button state management
- 📏 Proper window resizing with anchored controls

### 🐛 Known Issues

None at this time. Please report issues at: https://github.com/armaansood/CosmosDBIngester/issues

### 🙏 Acknowledgments

- Azure Cosmos DB team
- Bogus library by Brian Chavez
- Community feedback

---

**Full Changelog**: https://github.com/armaansood/CosmosDBIngester/commits/v1.0.0
```

5. **Attach Release Files**:
   - Drag and drop the ZIP file(s) you created in Step 2
   - GitHub will upload them as release assets

6. **Choose Options**:
   - ☐ Set as a pre-release (uncheck for stable release)
   - ☐ Set as the latest release (check this)
   - ☑ Create a discussion for this release (optional but recommended)

7. Click **Publish release**

## Step 4: Update README with Release Badge

Add this to the top of your README.md:

```markdown
[![Latest Release](https://img.shields.io/github/v/release/armaansood/CosmosDBIngester)](https://github.com/armaansood/CosmosDBIngester/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/armaansood/CosmosDBIngester/total)](https://github.com/armaansood/CosmosDBIngester/releases)
```

## Verification Checklist

- [ ] Release ZIP files created successfully
- [ ] All necessary DLL files included in ZIP
- [ ] CosmosDBIngester.exe runs on a clean Windows machine
- [ ] GitHub release created with proper version tag
- [ ] Release notes are clear and comprehensive
- [ ] Download links work correctly
- [ ] Documentation links are valid
- [ ] Release is marked as latest

## Future Releases

For subsequent releases:
1. Update version numbers (e.g., v1.1.0, v1.2.0)
2. Update CHANGELOG with new features/fixes
3. Follow the same build and release process
4. Include "**Full Changelog**" link comparing previous version to new version

---

**Next Steps**: After creating the release, share it with your users and monitor the Issues page for feedback!
