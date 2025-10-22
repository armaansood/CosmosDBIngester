using System.Security;

namespace CosmosDBIngester.Models;

public class CosmosConfig
{
    public string Endpoint { get; set; } = string.Empty;
    
    // Use SecureString for credential protection
    private SecureString? _primaryKey;
    
    public void SetPrimaryKey(string key)
    {
        _primaryKey?.Dispose();
        _primaryKey = new SecureString();
        foreach (char c in key)
        {
            _primaryKey.AppendChar(c);
        }
        _primaryKey.MakeReadOnly();
    }
    
    public string GetPrimaryKey()
    {
        if (_primaryKey == null) return string.Empty;
        
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(_primaryKey);
            return System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
            }
        }
    }
    
    public void ClearCredentials()
    {
        _primaryKey?.Dispose();
        _primaryKey = null;
    }
    
    public string DatabaseName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public int ThroughputRUs { get; set; } = 400;
    public int BatchSize { get; set; } = 10;
    public int DocumentSizeKB { get; set; } = 1;
    public string WorkloadType { get; set; } = "Sequential";
    public DataType DataType { get; set; } = DataType.Financial;
    
    // Security: Max RU budget to prevent excessive costs
    public int MaxRUBudgetPerSecond { get; set; } = 10000; // Default 10K RU/s max
}
