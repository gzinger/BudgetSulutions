namespace TransactionCategorizer.Services;

/// <summary>
/// Result from AI categorization
/// </summary>
public class AiCategorizationResult
{
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

/// <summary>
/// Interface for AI-based categorization
/// </summary>
public interface IAiCategorizationService
{
    Task<AiCategorizationResult?> CategorizeAsync(string description);
}
