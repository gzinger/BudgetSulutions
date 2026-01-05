namespace TransactionCategorizer.Models;

/// <summary>
/// Represents a unified transaction from any source (bank or credit card)
/// </summary>
public class Transaction
{
    public string SourceFile { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // "Bank" or "CreditCard"
    public string AccountId { get; set; } = string.Empty; // e.g., "1047", "3383"
    public DateTime TransactionDate { get; set; }
    public DateTime PostingDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OriginalType { get; set; } = string.Empty;
    public decimal? Balance { get; set; }
    public string? CheckNumber { get; set; }
    
    // Categorization
    public string TransactionType => Amount >= 0 ? "Credit" : "Debit";
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    
    // For tracking categorization source
    public string CategorizationSource { get; set; } = string.Empty; // "Original", "Pattern", "AI", "User"
    public string MatchedPattern { get; set; } = string.Empty; // The exact pattern that matched
    
    // Helper property for displaying balance
    public string BalanceDisplay => Balance.HasValue ? Balance.Value.ToString("C2") : "N/A";
    
    // Helper property for displaying categorization details
    public string CategorizationDetails => string.IsNullOrEmpty(MatchedPattern) 
        ? CategorizationSource 
        : $"{CategorizationSource}: {MatchedPattern}";
}

/// <summary>
/// Bank account transaction from CSV
/// </summary>
public class BankTransaction
{
    public string Details { get; set; } = string.Empty;
    public string PostingDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal? Balance { get; set; }
    public string? CheckOrSlipNumber { get; set; }
}

/// <summary>
/// Credit card transaction from CSV
/// </summary>
public class CreditCardTransaction
{
    public string TransactionDate { get; set; } = string.Empty;
    public string PostDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Memo { get; set; }
}
