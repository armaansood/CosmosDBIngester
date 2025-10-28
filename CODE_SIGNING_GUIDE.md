# 🛡️ Code Signing Guide for Windows Security

## Why Windows Shows "Unsafe" Warning

When you try to run `CosmosDBIngester.exe`, Windows SmartScreen shows a warning because:

1. **The executable is not digitally signed** with a code signing certificate
2. Windows cannot verify the publisher identity
3. The file doesn't have a reputation history with Microsoft

This is **NORMAL** for unsigned applications and doesn't mean the application is actually unsafe.

---

## ✅ How to Run the Unsigned Application (For Users)

### Option 1: Click "More Info" and "Run Anyway"
1. When you see the warning: "Windows protected your PC"
2. Click **"More info"**
3. Click **"Run anyway"**
4. The application will start

### Option 2: Right-Click Method
1. Right-click on `CosmosDBIngester.exe`
2. Select **"Properties"**
3. Check **"Unblock"** at the bottom
4. Click **"Apply"** then **"OK"**
5. Now double-click to run

### Option 3: PowerShell Unblock
```powershell
# Navigate to the folder
cd "path\to\CosmosDBIngester"

# Unblock the file
Unblock-File -Path .\CosmosDBIngester.exe

# Run the application
.\CosmosDBIngester.exe
```

### Option 4: Windows Defender Exclusion (For Development)
1. Open **Windows Security**
2. Go to **Virus & threat protection** → **Manage settings**
3. Scroll to **Exclusions** → **Add or remove exclusions**
4. Add the folder containing the application

---

## 🔐 Code Signing for Production (For Developers)

To remove the Windows warning permanently, you need to **digitally sign** the executable.

### Step 1: Obtain a Code Signing Certificate

#### Option A: Purchase from Certificate Authority (Production)
**Recommended CAs:**
- **Sectigo** (formerly Comodo) - $199-474/year
- **DigiCert** - $474-629/year  
- **GlobalSign** - $299-599/year

**Requirements:**
- Company registration documents
- Business validation
- 1-3 business days processing

**For Individual Developers:**
- Some CAs offer individual code signing certificates
- Requires ID verification
- Cheaper than OV certificates ($100-200/year)

#### Option B: Use Windows Store (Free but requires submission)
- Submit to Microsoft Store
- Automatic signing by Microsoft
- Requires Microsoft Store developer account ($19 one-time)

#### Option C: Self-Signed Certificate (Development/Testing Only)
```powershell
# Create self-signed certificate (NOT for production)
New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=CosmosDBIngester Development" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(5)

# Export certificate
$cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object {$_.Subject -like "*CosmosDBIngester*"}
Export-Certificate -Cert $cert -FilePath "CosmosDBIngester.cer"

# Install to Trusted Root (requires admin)
Import-Certificate -FilePath "CosmosDBIngester.cer" -CertStoreLocation "Cert:\LocalMachine\Root"
```

⚠️ **Warning:** Self-signed certificates still trigger warnings on other computers.

---

### Step 2: Sign the Executable

#### Using SignTool (Windows SDK)

**Install Windows SDK:**
```powershell
# Download from: https://developer.microsoft.com/windows/downloads/windows-sdk/
# Or install via Visual Studio Installer
```

**Sign the executable:**
```powershell
# Navigate to Windows SDK bin folder (adjust version as needed)
cd "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64"

# Sign with PFX file (password-protected certificate)
.\signtool.exe sign /f "C:\path\to\certificate.pfx" /p "YourPassword" /tr http://timestamp.digicert.com /td sha256 /fd sha256 "C:\path\to\CosmosDBIngester.exe"

# Sign with certificate from store (more secure)
.\signtool.exe sign /n "Your Certificate Name" /tr http://timestamp.digicert.com /td sha256 /fd sha256 "C:\path\to\CosmosDBIngester.exe"

# Verify signature
.\signtool.exe verify /pa "C:\path\to\CosmosDBIngester.exe"
```

**Parameters Explained:**
- `/f` - Certificate file (PFX)
- `/p` - Password for PFX
- `/n` - Certificate subject name (if using certificate store)
- `/tr` - Timestamp server (proves when code was signed)
- `/td` - Timestamp digest algorithm (SHA256)
- `/fd` - File digest algorithm (SHA256)
- `/pa` - Verify with default policy

**Important Timestamp Servers:**
- DigiCert: `http://timestamp.digicert.com`
- Sectigo: `http://timestamp.sectigo.com`
- GlobalSign: `http://timestamp.globalsign.com`

---

### Step 3: Automated Signing in Build Process

#### Add to .csproj (Post-Build Event)
```xml
<Target Name="SignOutput" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
  <Exec Command="signtool.exe sign /f &quot;$(SolutionDir)certificate.pfx&quot; /p &quot;$(CertPassword)&quot; /tr http://timestamp.digicert.com /td sha256 /fd sha256 &quot;$(TargetPath)&quot;" />
</Target>
```

#### Create PowerShell Build Script
```powershell
# build-and-sign.ps1

param(
    [string]$CertificatePath = "certificate.pfx",
    [string]$CertificatePassword = "",
    [string]$Configuration = "Release"
)

# Build the project
Write-Host "Building project..." -ForegroundColor Green
dotnet build -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Find the executable
$exePath = ".\bin\$Configuration\net8.0-windows\CosmosDBIngester.exe"

if (Test-Path $exePath) {
    Write-Host "Signing executable..." -ForegroundColor Green
    
    # Sign the executable
    & "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" sign `
        /f $CertificatePath `
        /p $CertificatePassword `
        /tr http://timestamp.digicert.com `
        /td sha256 `
        /fd sha256 `
        $exePath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Successfully signed!" -ForegroundColor Green
        
        # Verify signature
        & "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe" verify /pa $exePath
    } else {
        Write-Host "Signing failed!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Executable not found: $exePath" -ForegroundColor Red
    exit 1
}

Write-Host "Done!" -ForegroundColor Green
```

**Usage:**
```powershell
.\build-and-sign.ps1 -CertificatePath "C:\certs\mycert.pfx" -CertificatePassword "MyPassword123"
```

---

### Step 4: Verify Code Signature

#### Check Signature Properties
1. Right-click on `CosmosDBIngester.exe`
2. Select **"Properties"**
3. Go to **"Digital Signatures"** tab
4. You should see your certificate details

#### PowerShell Verification
```powershell
# Check signature status
Get-AuthenticodeSignature -FilePath "CosmosDBIngester.exe" | Format-List *

# Expected output for valid signature:
# Status: Valid
# SignerCertificate: [Your certificate details]
# TimeStamperCertificate: [Timestamp certificate]
```

---

## 🔒 Security Best Practices for Code Signing

### Certificate Security
1. **Never commit certificates to Git**
   - Already in `.gitignore`: `*.pfx`, `*.p12`, `*certificate*`
   
2. **Use hardware tokens for production**
   - USB tokens (YubiKey, etc.)
   - More secure than file-based certificates
   - Cannot be copied or stolen remotely

3. **Store passwords in Azure Key Vault or similar**
   - Don't hardcode passwords in scripts
   - Use environment variables: `$env:CERT_PASSWORD`

4. **Use separate certificates for dev and production**
   - Self-signed for development
   - CA-issued for production releases

### Timestamping is Critical
- Always use timestamp servers (`/tr` parameter)
- Without timestamp, signature expires when certificate expires
- With timestamp, signature remains valid even after certificate expiry
- Use multiple timestamp servers for redundancy

### Regular Certificate Rotation
- Renew certificates before expiry
- Update build scripts with new certificates
- Test signing process before old certificate expires

---

## 💰 Cost Comparison

| Option | Cost | Pros | Cons |
|--------|------|------|------|
| **Self-Signed** | Free | Free, instant | Not trusted, still shows warnings |
| **Individual Code Signing** | $100-200/year | Affordable, removes warnings | Requires ID verification |
| **OV Code Signing** | $200-500/year | Trusted, professional | Requires business validation |
| **EV Code Signing** | $300-700/year | Highest trust, immediate SmartScreen reputation | Expensive, requires hardware token |
| **Microsoft Store** | $19 one-time | Free signing, distribution platform | Must follow Store policies |

---

## 🚀 Quick Start for This Application

### For Users (No Code Signing)
**Simplest approach:**
```powershell
# Unblock the downloaded file
Unblock-File -Path ".\CosmosDBIngester.exe"

# Run it
.\CosmosDBIngester.exe
```

### For Developers (Production Signing)
1. **Purchase certificate** from Sectigo/DigiCert
2. **Install certificate** to your machine
3. **Add to build script:**
   ```powershell
   dotnet build -c Release
   signtool.exe sign /n "Your Company Name" /tr http://timestamp.digicert.com /td sha256 /fd sha256 "bin\Release\net8.0-windows\CosmosDBIngester.exe"
   ```

---

## 📋 Checklist for Production Release

- [ ] Obtain code signing certificate from CA
- [ ] Install certificate securely (use hardware token if possible)
- [ ] Update build process to sign executable
- [ ] Verify signature after building
- [ ] Test on clean Windows machine
- [ ] Document signing process for team
- [ ] Set up certificate renewal reminder
- [ ] Store certificate backup securely

---

## 🆘 Troubleshooting

### "SignTool not found"
**Solution:** Install Windows SDK
```powershell
# Check if SignTool exists
Get-Command signtool -ErrorAction SilentlyContinue

# If not found, add to PATH or use full path
$env:PATH += ";C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64"
```

### "Certificate not found"
**Solution:** Check certificate store
```powershell
# List all code signing certificates
Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert
Get-ChildItem -Path Cert:\LocalMachine\My -CodeSigningCert
```

### "Timestamp server unavailable"
**Solution:** Use alternative timestamp server
```powershell
# Try different timestamp servers
# DigiCert: http://timestamp.digicert.com
# Sectigo: http://timestamp.sectigo.com
# GlobalSign: http://timestamp.globalsign.com
```

### Still shows warning after signing
**Possible reasons:**
1. Certificate not from trusted CA (self-signed)
2. Certificate chain incomplete
3. No timestamp applied
4. File modified after signing

**Verify:**
```powershell
Get-AuthenticodeSignature "CosmosDBIngester.exe"
# Status should be "Valid"
```

---

## 📚 Additional Resources

- [Microsoft Code Signing Best Practices](https://docs.microsoft.com/windows/win32/seccrypto/cryptography-tools)
- [SignTool Documentation](https://docs.microsoft.com/windows/win32/seccrypto/signtool)
- [Windows App Certification Kit](https://developer.microsoft.com/windows/downloads/windows-app-certification-kit/)
- [Azure Code Signing](https://azure.microsoft.com/services/azure-code-signing/)

---

## 🎯 Recommendation

**For this open-source project:**
1. **Short-term:** Users can unblock the file (documented in README)
2. **Medium-term:** Consider GitHub Sponsors to fund code signing certificate
3. **Long-term:** Publish to Microsoft Store for free Microsoft signing

**For enterprise deployment:**
- Purchase EV Code Signing certificate ($300-700/year)
- Use hardware token for maximum security
- Implement automated signing in CI/CD pipeline

---

**Current Status:** Application is safe but unsigned. Windows warning is expected behavior for all unsigned applications.

