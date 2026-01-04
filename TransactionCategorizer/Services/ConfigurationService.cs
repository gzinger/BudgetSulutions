using System.Text.Json;
using TransactionCategorizer.Models;

namespace TransactionCategorizer.Services;

/// <summary>
/// Manages application configuration and category definitions
/// </summary>
public class ConfigurationService
{
    private readonly string _baseDirectory;
    private readonly string _appSettingsPath;
    private AppSettings _settings;
    private CategoriesConfiguration? _categories;

    public ConfigurationService()
    {
        _baseDirectory = GetBaseDirectory();
        _appSettingsPath = Path.Combine(_baseDirectory, "appsettings.json");
        _settings = LoadAppSettings();
    }

    private string GetBaseDirectory()
    {
        var location = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                      ?? Environment.CurrentDirectory;

        // If running from bin folder, go up to find project root
        if (location.Contains("bin"))
        {
            return Path.GetFullPath(Path.Combine(location, "..", "..", ".."));
        }

        return location;
    }

    private AppSettings LoadAppSettings()
    {
        if (File.Exists(_appSettingsPath))
        {
            try
            {
                var json = File.ReadAllText(_appSettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
        return new AppSettings();
    }

    private void SaveAppSettings()
    {
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_appSettingsPath, json);
    }

    public string GetConfigDirectory(string? commandLineArg)
    {
        // Priority: 1. Command line, 2. Saved setting, 3. Base directory, 4. Prompt user
        if (!string.IsNullOrEmpty(commandLineArg) && Directory.Exists(commandLineArg))
        {
            _settings.ConfigDirectory = commandLineArg;
            SaveAppSettings();
            return commandLineArg;
        }

        if (!string.IsNullOrEmpty(_settings.ConfigDirectory) && Directory.Exists(_settings.ConfigDirectory))
        {
            return _settings.ConfigDirectory;
        }

        if (File.Exists(Path.Combine(_baseDirectory, "categories.json")))
        {
            _settings.ConfigDirectory = _baseDirectory;
            SaveAppSettings();
            return _baseDirectory;
        }

        // Prompt user
        Console.WriteLine("\nConfiguration directory not found.");
        Console.WriteLine("This directory should contain 'categories.json' and 'category_mappings.json'");
        Console.Write("Enter configuration directory path: ");
        var input = Console.ReadLine()?.Trim();

        if (!string.IsNullOrEmpty(input) && Directory.Exists(input))
        {
            _settings.ConfigDirectory = input;
            SaveAppSettings();
            return input;
        }

        // Fallback to base directory
        _settings.ConfigDirectory = _baseDirectory;
        SaveAppSettings();
        return _baseDirectory;
    }

    public string GetDataDirectory(string? commandLineArg)
    {
        // Priority: 1. Command line, 2. Saved setting, 3. Base directory, 4. Prompt user
        if (!string.IsNullOrEmpty(commandLineArg) && Directory.Exists(commandLineArg))
        {
            _settings.DataDirectory = commandLineArg;
            SaveAppSettings();
            return commandLineArg;
        }

        if (!string.IsNullOrEmpty(_settings.DataDirectory) && Directory.Exists(_settings.DataDirectory))
        {
            return _settings.DataDirectory;
        }

        // Check if CSV files exist in base directory
        var csvFiles = Directory.GetFiles(_baseDirectory, "*.CSV", SearchOption.TopDirectoryOnly);
        if (csvFiles.Length > 0)
        {
            _settings.DataDirectory = _baseDirectory;
            SaveAppSettings();
            return _baseDirectory;
        }

        // Prompt user
        Console.WriteLine("\nData directory not found.");
        Console.WriteLine("This directory should contain your CSV transaction files.");
        Console.Write("Enter data directory path: ");
        var input = Console.ReadLine()?.Trim();

        if (!string.IsNullOrEmpty(input) && Directory.Exists(input))
        {
            _settings.DataDirectory = input;
            SaveAppSettings();
            return input;
        }

        // Fallback to base directory
        _settings.DataDirectory = _baseDirectory;
        SaveAppSettings();
        return _baseDirectory;
    }

    public CategoriesConfiguration LoadCategories(string configDirectory)
    {
        if (_categories != null)
            return _categories;

        var categoriesPath = Path.Combine(configDirectory, "categories.json");
        
        if (File.Exists(categoriesPath))
        {
            try
            {
                var json = File.ReadAllText(categoriesPath);
                _categories = JsonSerializer.Deserialize<CategoriesConfiguration>(json);
                if (_categories != null)
                    return _categories;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        // Create default categories if file doesn't exist
        _categories = CreateDefaultCategories();
        SaveCategories(configDirectory);
        return _categories;
    }

    private CategoriesConfiguration CreateDefaultCategories()
    {
        return new CategoriesConfiguration
        {
            Categories = new List<CategoryDefinition>
            {
                new() { Name = "Income", Subcategories = new() { "Salary", "Interest", "Refund", "Other" } },
                new() { Name = "Housing", Subcategories = new() { "Mortgage", "Rent", "HOA", "Repairs", "Other" } },
                new() { Name = "Utilities", Subcategories = new() { "Electric", "Gas", "Water", "Internet/Cable", "Phone" } },
                new() { Name = "Transportation", Subcategories = new() { "Gas/Fuel", "Tolls", "Parking", "Public Transit", "Other" } },
                new() { Name = "Groceries", Subcategories = new() { "Supermarket", "Specialty", "Wholesale", "Other" } },
                new() { Name = "Food & Dining", Subcategories = new() { "Restaurant", "Fast Food", "Takeout", "Coffee", "Other" } },
                new() { Name = "Shopping", Subcategories = new() { "Online", "Retail", "Office Supplies", "Books", "Other" } },
                new() { Name = "Health & Wellness", Subcategories = new() { "Pharmacy", "Medical", "Dental", "Fitness", "Other" } },
                new() { Name = "Insurance", Subcategories = new() { "Life", "Home", "Auto", "Health", "Other" } },
                new() { Name = "Gifts & Donations", Subcategories = new() { "Religious", "Charity", "Education", "Gifts", "Other" } },
                new() { Name = "Financial", Subcategories = new() { "Transfer", "Payment", "Fee", "ATM", "Other" } },
                new() { Name = "Other", Subcategories = new() { "Entertainment", "Services", "Personal", "Miscellaneous", "Uncategorized" } }
            },
            LastUpdated = DateTime.Now
        };
    }

    public void SaveCategories(string configDirectory)
    {
        if (_categories == null)
            return;

        _categories.LastUpdated = DateTime.Now;
        var categoriesPath = Path.Combine(configDirectory, "categories.json");
        var json = JsonSerializer.Serialize(_categories, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(categoriesPath, json);
    }

    public void AddSubcategoryIfNeeded(string category, string subcategory, string configDirectory)
    {
        if (_categories == null)
            return;

        var categoryDef = _categories.Categories.FirstOrDefault(c => 
            c.Name.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (categoryDef != null)
        {
            // Check if subcategory already exists (case-insensitive)
            if (!categoryDef.Subcategories.Any(s => 
                s.Equals(subcategory, StringComparison.OrdinalIgnoreCase)))
            {
                categoryDef.Subcategories.Add(subcategory);
                SaveCategories(configDirectory);
                Console.WriteLine($"  [New subcategory '{subcategory}' added to '{category}']");
            }
        }
    }
}
