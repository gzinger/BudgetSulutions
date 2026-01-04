namespace TransactionCategorizer.Models;

/// <summary>
/// Represents a category mapping learned from user input
/// </summary>
public class CategoryMapping
{
    public string Pattern { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

/// <summary>
/// Stored mappings file structure
/// </summary>
public class CategoryMappings
{
    public List<CategoryMapping> Mappings { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
