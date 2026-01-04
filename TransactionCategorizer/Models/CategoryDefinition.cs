namespace TransactionCategorizer.Models;

/// <summary>
/// Represents a category with its subcategories
/// </summary>
public class CategoryDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<string> Subcategories { get; set; } = new();
    public List<SubcategoryRule>? InferenceRules { get; set; }
}

/// <summary>
/// Rule for inferring subcategory from transaction description
/// </summary>
public class SubcategoryRule
{
    public string Subcategory { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
}

/// <summary>
/// Root object for categories.json
/// </summary>
public class CategoriesConfiguration
{
    public List<CategoryDefinition> Categories { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
