using System.Text.RegularExpressions;

namespace CosmosDBIngester.Security;

/// <summary>
/// Comprehensive input validation to prevent injection attacks and ensure data integrity
/// </summary>
public static class InputValidator
{
    // Security: Strict validation patterns
    private static readonly Regex EndpointPattern = new Regex(
        @"^https://[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.documents\.azure\.com:443/?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    
    private static readonly Regex DatabaseNamePattern = new Regex(
        @"^[a-zA-Z0-9][a-zA-Z0-9_-]{0,254}$",
        RegexOptions.Compiled
    );
    
    private static readonly Regex CollectionNamePattern = new Regex(
        @"^[a-zA-Z0-9][a-zA-Z0-9_-]{0,254}$",
        RegexOptions.Compiled
    );
    
    private static readonly Regex PrimaryKeyPattern = new Regex(
        @"^[a-zA-Z0-9+/]{86}==$",  // Base64 format for Cosmos DB keys
        RegexOptions.Compiled
    );
    
    // Security: Max length constraints
    private const int MaxEndpointLength = 2048;
    private const int MaxDatabaseNameLength = 255;
    private const int MaxCollectionNameLength = 255;
    private const int MaxPrimaryKeyLength = 256;
    
    public static ValidationResult ValidateEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return ValidationResult.Error("Endpoint cannot be empty.");
        
        endpoint = endpoint.Trim();
        
        if (endpoint.Length > MaxEndpointLength)
            return ValidationResult.Error($"Endpoint exceeds maximum length of {MaxEndpointLength} characters.");
        
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return ValidationResult.Error("Endpoint must be a valid URL.");
        
        // Security: HTTPS ONLY - reject HTTP
        if (uri.Scheme != "https")
            return ValidationResult.Error("Endpoint must use HTTPS protocol. HTTP is not allowed for security reasons.");
        
        // Security: Validate Cosmos DB endpoint format
        if (!EndpointPattern.IsMatch(endpoint))
            return ValidationResult.Error("Endpoint must be a valid Azure Cosmos DB endpoint (e.g., https://your-account.documents.azure.com:443/)");
        
        return ValidationResult.Success();
    }
    
    public static ValidationResult ValidatePrimaryKey(string primaryKey)
    {
        if (string.IsNullOrWhiteSpace(primaryKey))
            return ValidationResult.Error("Primary key cannot be empty.");
        
        primaryKey = primaryKey.Trim();
        
        if (primaryKey.Length > MaxPrimaryKeyLength)
            return ValidationResult.Error($"Primary key exceeds maximum length of {MaxPrimaryKeyLength} characters.");
        
        // Security: Validate key format (Cosmos DB uses base64-encoded 88-character keys)
        if (!PrimaryKeyPattern.IsMatch(primaryKey))
            return ValidationResult.Error("Primary key format is invalid. Must be a valid Cosmos DB primary key.");
        
        return ValidationResult.Success();
    }
    
    public static ValidationResult ValidateDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
            return ValidationResult.Error("Database name cannot be empty.");
        
        databaseName = databaseName.Trim();
        
        if (databaseName.Length > MaxDatabaseNameLength)
            return ValidationResult.Error($"Database name exceeds maximum length of {MaxDatabaseNameLength} characters.");
        
        // Security: Prevent injection with character whitelist
        if (!DatabaseNamePattern.IsMatch(databaseName))
            return ValidationResult.Error("Database name contains invalid characters. Only alphanumeric, hyphen, and underscore are allowed.");
        
        // Security: Prevent SQL/NoSQL injection keywords
        var dangerousKeywords = new[] { "--", "/*", "*/", ";", "drop", "delete", "truncate", "exec", "execute" };
        if (dangerousKeywords.Any(kw => databaseName.ToLowerInvariant().Contains(kw)))
            return ValidationResult.Error("Database name contains potentially dangerous characters or keywords.");
        
        return ValidationResult.Success();
    }
    
    public static ValidationResult ValidateCollectionName(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
            return ValidationResult.Error("Collection name cannot be empty.");
        
        collectionName = collectionName.Trim();
        
        if (collectionName.Length > MaxCollectionNameLength)
            return ValidationResult.Error($"Collection name exceeds maximum length of {MaxCollectionNameLength} characters.");
        
        // Security: Prevent injection with character whitelist
        if (!CollectionNamePattern.IsMatch(collectionName))
            return ValidationResult.Error("Collection name contains invalid characters. Only alphanumeric, hyphen, and underscore are allowed.");
        
        // Security: Prevent SQL/NoSQL injection keywords
        var dangerousKeywords = new[] { "--", "/*", "*/", ";", "drop", "delete", "truncate", "exec", "execute" };
        if (dangerousKeywords.Any(kw => collectionName.ToLowerInvariant().Contains(kw)))
            return ValidationResult.Error("Collection name contains potentially dangerous characters or keywords.");
        
        return ValidationResult.Success();
    }
    
    public static ValidationResult ValidateThroughput(int throughput)
    {
        if (throughput < 400)
            return ValidationResult.Error("Throughput must be at least 400 RU/s.");
        
        if (throughput > 1000000)
            return ValidationResult.Error("Throughput cannot exceed 1,000,000 RU/s for safety.");
        
        return ValidationResult.Success();
    }
    
    public static ValidationResult ValidateBatchSize(int batchSize)
    {
        if (batchSize < 1)
            return ValidationResult.Error("Batch size must be at least 1.");
        
        if (batchSize > 1000)
            return ValidationResult.Error("Batch size cannot exceed 1,000 for safety.");
        
        return ValidationResult.Success();
    }
    
    public static ValidationResult ValidateDocumentSize(int sizeKB)
    {
        if (sizeKB < 1)
            return ValidationResult.Error("Document size must be at least 1 KB.");
        
        if (sizeKB > 2048)
            return ValidationResult.Error("Document size cannot exceed 2048 KB (2 MB).");
        
        return ValidationResult.Success();
    }
}

public class ValidationResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    
    private ValidationResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }
    
    public static ValidationResult Success() => new ValidationResult(true);
    public static ValidationResult Error(string message) => new ValidationResult(false, message);
}
