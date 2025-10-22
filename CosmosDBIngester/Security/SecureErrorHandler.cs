namespace CosmosDBIngester.Security;

/// <summary>
/// Handles error messages securely by sanitizing sensitive information
/// Provides user-friendly messages while logging detailed errors securely
/// </summary>
public static class SecureErrorHandler
{
    /// <summary>
    /// Gets a user-friendly error message without exposing sensitive details
    /// </summary>
    public static string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            ArgumentException => "Invalid input. Please check your connection settings.",
            UnauthorizedAccessException => "Access denied. Please verify your credentials.",
            TimeoutException => "The operation timed out. Please check your network connection.",
            InvalidOperationException => "An invalid operation was attempted. Please try again.",
            OperationCanceledException => "The operation was cancelled.",
            _ when ex.Message.Contains("Unauthorized") => "Authentication failed. Please verify your primary key.",
            _ when ex.Message.Contains("NotFound") => "The specified resource was not found.",
            _ when ex.Message.Contains("Forbidden") => "Access to this resource is forbidden.",
            _ when ex.Message.Contains("TooManyRequests") || ex.Message.Contains("429") => 
                "Rate limit exceeded. Please wait a moment and try again.",
            _ when ex.Message.Contains("ServiceUnavailable") || ex.Message.Contains("503") => 
                "The service is temporarily unavailable. Please try again later.",
            _ when ex.Message.Contains("Conflict") || ex.Message.Contains("409") => 
                "A conflict occurred. The resource may already exist.",
            _ => "An unexpected error occurred. Please check the logs for more details."
        };
    }
    
    /// <summary>
    /// Gets error category for logging purposes
    /// </summary>
    public static string GetErrorCategory(Exception ex)
    {
        return ex switch
        {
            ArgumentException => "Validation",
            UnauthorizedAccessException => "Authentication",
            TimeoutException => "Network",
            InvalidOperationException => "Operation",
            OperationCanceledException => "Cancellation",
            _ when ex.Message.Contains("Unauthorized") => "Authentication",
            _ when ex.Message.Contains("NotFound") => "Resource",
            _ when ex.Message.Contains("Forbidden") => "Authorization",
            _ when ex.Message.Contains("TooManyRequests") || ex.Message.Contains("429") => "RateLimit",
            _ when ex.Message.Contains("ServiceUnavailable") || ex.Message.Contains("503") => "ServiceAvailability",
            _ when ex.Message.Contains("Conflict") || ex.Message.Contains("409") => "Conflict",
            _ => "Unknown"
        };
    }
    
    /// <summary>
    /// Determines if the error should trigger a security alert
    /// </summary>
    public static bool IsSecuritySensitive(Exception ex)
    {
        var sensitiveKeywords = new[]
        {
            "Unauthorized", "Forbidden", "Authentication", "Authorization",
            "401", "403", "key", "token", "credential"
        };
        
        var message = ex.Message.ToLowerInvariant();
        return sensitiveKeywords.Any(kw => message.Contains(kw.ToLowerInvariant()));
    }
    
    /// <summary>
    /// Gets severity level for the exception
    /// </summary>
    public static string GetSeverity(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => "Critical",
            ArgumentException => "Warning",
            TimeoutException => "Warning",
            OperationCanceledException => "Info",
            _ when IsSecuritySensitive(ex) => "Critical",
            _ => "Error"
        };
    }
}
