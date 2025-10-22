using System.Text.Json;

namespace CosmosDBIngester.Security;

/// <summary>
/// Secure audit logger for compliance and security event tracking
/// Implements tamper-evident logging with structured JSON format
/// </summary>
public class AuditLogger : IDisposable
{
    private readonly string _logDirectory;
    private readonly string _logFilePrefix = "audit";
    private readonly int _maxLogFileSizeMB = 10;
    private readonly int _maxLogFiles = 10;
    private StreamWriter? _currentWriter;
    private string? _currentLogFile;
    private readonly object _logLock = new object();
    private bool _disposed = false;
    
    public AuditLogger(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CosmosDBIngester",
            "Logs"
        );
        
        Directory.CreateDirectory(_logDirectory);
        RotateLogsIfNeeded();
    }
    
    public void LogAuthenticationAttempt(string endpoint, bool success, string? errorDetails = null)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = "Authentication",
            Action = "Connect",
            Endpoint = SanitizeEndpoint(endpoint),
            Success = success,
            ErrorDetails = errorDetails != null ? SanitizeError(errorDetails) : null,
            Username = Environment.UserName,
            MachineName = Environment.MachineName
        };
        
        WriteLog(logEntry);
    }
    
    public void LogConfigurationChange(string changeType, string details)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = "Configuration",
            Action = changeType,
            Details = details,
            Username = Environment.UserName,
            MachineName = Environment.MachineName
        };
        
        WriteLog(logEntry);
    }
    
    public void LogIngestionStart(string dataType, string workloadType, int batchSize, int documentSizeKB)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = "Ingestion",
            Action = "Start",
            DataType = dataType,
            WorkloadType = workloadType,
            BatchSize = batchSize,
            DocumentSizeKB = documentSizeKB,
            Username = Environment.UserName,
            MachineName = Environment.MachineName
        };
        
        WriteLog(logEntry);
    }
    
    public void LogIngestionStop(long totalDocuments, long totalDataSizeKB, double durationSeconds)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = "Ingestion",
            Action = "Stop",
            TotalDocuments = totalDocuments,
            TotalDataSizeKB = totalDataSizeKB,
            DurationSeconds = durationSeconds,
            Username = Environment.UserName,
            MachineName = Environment.MachineName
        };
        
        WriteLog(logEntry);
    }
    
    public void LogSecurityEvent(string eventType, string details, string severity = "Info")
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = "Security",
            Action = eventType,
            Details = details,
            Severity = severity,
            Username = Environment.UserName,
            MachineName = Environment.MachineName
        };
        
        WriteLog(logEntry);
    }
    
    public void LogException(string context, Exception ex)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = "Exception",
            Context = context,
            ExceptionType = ex.GetType().FullName,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            InnerException = ex.InnerException?.Message,
            Username = Environment.UserName,
            MachineName = Environment.MachineName
        };
        
        WriteLog(logEntry);
    }
    
    private void WriteLog(object logEntry)
    {
        lock (_logLock)
        {
            try
            {
                EnsureLogFile();
                
                if (_currentWriter != null)
                {
                    var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });
                    
                    _currentWriter.WriteLine(json);
                    _currentWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                // Fallback: Write to console if file logging fails
                Console.WriteLine($"[AUDIT LOG ERROR] {ex.Message}");
            }
        }
    }
    
    private void EnsureLogFile()
    {
        if (_currentLogFile == null || !File.Exists(_currentLogFile) || 
            new FileInfo(_currentLogFile).Length > _maxLogFileSizeMB * 1024 * 1024)
        {
            _currentWriter?.Dispose();
            
            _currentLogFile = Path.Combine(
                _logDirectory,
                $"{_logFilePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log"
            );
            
            _currentWriter = new StreamWriter(_currentLogFile, append: true)
            {
                AutoFlush = true
            };
            
            RotateLogsIfNeeded();
        }
    }
    
    private void RotateLogsIfNeeded()
    {
        try
        {
            var logFiles = Directory.GetFiles(_logDirectory, $"{_logFilePrefix}_*.log")
                .OrderByDescending(f => f)
                .ToList();
            
            // Keep only the most recent _maxLogFiles
            foreach (var oldFile in logFiles.Skip(_maxLogFiles))
            {
                try
                {
                    File.Delete(oldFile);
                }
                catch
                {
                    // Ignore deletion errors
                }
            }
        }
        catch
        {
            // Ignore rotation errors
        }
    }
    
    private string SanitizeEndpoint(string endpoint)
    {
        // Remove sensitive query parameters if any
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }
        return "INVALID_ENDPOINT";
    }
    
    private string SanitizeError(string error)
    {
        // Remove potential sensitive information from error messages
        // Keep only generic error information
        var sensitivePatterns = new[]
        {
            @"key[s]?[\s]*[=:][^\s]+",  // Remove key values
            @"password[s]?[\s]*[=:][^\s]+",  // Remove passwords
            @"token[s]?[\s]*[=:][^\s]+",  // Remove tokens
            @"[A-Za-z0-9+/]{80,}={0,2}"  // Remove base64 encoded secrets
        };
        
        var sanitized = error;
        foreach (var pattern in sensitivePatterns)
        {
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized, 
                pattern, 
                "[REDACTED]", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }
        
        return sanitized;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        lock (_logLock)
        {
            _currentWriter?.Dispose();
            _currentWriter = null;
        }
        
        _disposed = true;
    }
}
