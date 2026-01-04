namespace TransactionCategorizer.Models;

/// <summary>
/// Represents a category with its subcategories
/// </summary>
public class CategoryDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<string> Subcategories { get; set; } = new();
}

/// <summary>
/// Root object for categories.json
/// </summary>
public class CategoriesConfiguration
{
    public List<CategoryDefinition> Categories { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
