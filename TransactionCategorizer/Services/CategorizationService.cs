using System.Text.Json;
using System.Text.RegularExpressions;
using TransactionCategorizer.Models;

namespace TransactionCategorizer.Services;

public class CategorizationService
{
    private readonly string _mappingsFilePath;
    private CategoryMappings _mappings;
    private readonly IAiCategorizationService? _aiService;
    private readonly ConfigurationService _configService;
    private readonly string _configDirectory;
    private CategoriesConfiguration _categoriesConfig;

    // Predefined patterns for common/well-known transactions only
    // Specific/local merchants should be in category_mappings.json
    private static readonly List<(string Pattern, string Category, string Subcategory, string Provider)> DefaultPatterns = new()
    {
        // Utilities - Well-known providers
        (@"CONSOLIDATED EDISON|CON ?ED", "Utilities", "Electric", "Con Edison"),
        (@"NATIONAL GRID|KEYSPAN", "Utilities", "Gas", "National Grid"),
        (@"OPTIMUM", "Utilities", "Internet/Cable", "Optimum"),
        (@"OOMA", "Utilities", "Phone", "Ooma"),
        
        // Banking/Transfers - Generic patterns
        (@"ODP TRANSFER|Online Transfer", "Financial", "Transfer", "Chase"),
        (@"INTEREST PAYMENT", "Income", "Interest", "Chase"),
        (@"CHASE CREDIT CRD AUTOPAY", "Financial", "Payment", "Chase"),
        (@"Zelle payment", "Financial", "Transfer", "Chase"),
        (@"ATM WITHDRAWAL|ATM W/D", "Financial", "ATM", "Chase"),
        (@"Payment to Chase card", "Financial", "Payment", "Chase"),
        (@"Credit Return", "Income", "Refund", ""),
        (@"MONEYGRAM", "Financial", "Transfer", "MoneyGram"),
        (@"BILLPAY|Bill Pay|Online Payment.*To", "Financial", "Payment", ""),
        (@"AUTOMATIC PAYMENT.*THANK", "Financial", "Payment", "Chase"),
        (@"LOAN_PMT|LOAN PMT", "Financial", "Payment", ""),
        
        // Income - Generic patterns
        (@"PAYROLL|DIRDEP|Direct Deposit", "Income", "Salary", ""),
        
        // Transportation - Well-known providers
        (@"E-Z\*?PASS|EZPASS", "Transportation", "Tolls", "EZ-Pass"),
        (@"SUNOCO|EXXON|SHELL|BP|MOBIL|CHEVRON|TEXACO", "Transportation", "Gas/Fuel", ""),
        (@"NYCDOT PARKING", "Transportation", "Parking", "NYC DOT"),
        
        // Shopping - Major retailers
        (@"AMAZON|AMZN|Amazon", "Shopping", "Online", "Amazon"),
        (@"eBay", "Shopping", "Online", "eBay"),
        (@"STAPLES", "Shopping", "Office Supplies", "Staples"),
        (@"WALMART|WAL-MART", "Shopping", "Retail", "Walmart"),
        (@"TARGET", "Shopping", "Retail", "Target"),
        (@"COSTCO", "Shopping", "Wholesale", "Costco"),
        
        // Generic category patterns from credit card data
        (@"Gas", "Transportation", "Gas/Fuel", ""),
        (@"Travel", "Transportation", "Other", ""),
    };

    public CategorizationService(string mappingsFilePath, ConfigurationService configService, string configDirectory, IAiCategorizationService? aiService = null)
    {
        _mappingsFilePath = mappingsFilePath;
        _aiService = aiService;
        _configService = configService;
        _configDirectory = configDirectory;
        _categoriesConfig = _configService.LoadCategories(configDirectory);
        _mappings = LoadMappings();
    }

    private CategoryMappings LoadMappings()
    {
        if (File.Exists(_mappingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_mappingsFilePath);
                return JsonSerializer.Deserialize<CategoryMappings>(json) ?? new CategoryMappings();
            }
            catch
            {
                return new CategoryMappings();
            }
        }
        return new CategoryMappings();
    }

    public void SaveMappings()
    {
        _mappings.LastUpdated = DateTime.Now;
        var json = JsonSerializer.Serialize(_mappings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_mappingsFilePath, json);
    }

    /// <summary>
    /// Adds a new mapping and immediately saves to file
    /// </summary>
    private void AddAndSaveMapping(string pattern, string category, string subcategory, string provider)
    {
        // Check if similar pattern already exists to avoid duplicates
        if (!_mappings.Mappings.Any(m => m.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            _mappings.Mappings.Add(new CategoryMapping
            {
                Pattern = pattern,
                Category = category,
                Subcategory = subcategory,
                Provider = provider
            });
            SaveMappings();
            Console.WriteLine($"  [Mapping saved for future use]");
        }
    }

    public async Task CategorizeTransactionsAsync(List<Transaction> transactions, bool useAi = true)
    {
        var uncategorized = new List<Transaction>();

        foreach (var transaction in transactions)
        {
            // Skip if already categorized from source file
            if (!string.IsNullOrEmpty(transaction.Category) && transaction.CategorizationSource == "Original")
            {
                ExtractProviderFromDescription(transaction);
                // Still need subcategory
                if (string.IsNullOrEmpty(transaction.Subcategory))
                {
                    transaction.Subcategory = DetermineSubcategory(transaction);
                }
                continue;
            }

            // Try user mappings first (from file)
            if (TryApplyUserMapping(transaction))
                continue;

            // Try default patterns
            if (TryApplyDefaultPattern(transaction))
                continue;

            uncategorized.Add(transaction);
        }

        // Process uncategorized with AI or user input
        if (uncategorized.Any())
        {
            Console.WriteLine($"\nFound {uncategorized.Count} uncategorized transactions.");
            Console.WriteLine("Options: Enter category info, 's' to skip, 'a' to skip all remaining.\n");
            await ProcessUncategorizedAsync(uncategorized, useAi);
        }
    }

    private bool TryApplyUserMapping(Transaction transaction)
    {
        foreach (var mapping in _mappings.Mappings)
        {
            try
            {
                if (Regex.IsMatch(transaction.Description, mapping.Pattern, RegexOptions.IgnoreCase))
                {
                    transaction.Category = mapping.Category;
                    transaction.Subcategory = mapping.Subcategory;
                    transaction.Provider = string.IsNullOrEmpty(mapping.Provider) 
                        ? ExtractProviderName(transaction.Description) 
                        : mapping.Provider;
                    transaction.CategorizationSource = "UserMapping";
                    return true;
                }
            }
            catch (RegexParseException)
            {
                // If pattern is invalid regex, try simple contains match
                if (transaction.Description.Contains(mapping.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    transaction.Category = mapping.Category;
                    transaction.Subcategory = mapping.Subcategory;
                    transaction.Provider = string.IsNullOrEmpty(mapping.Provider) 
                        ? ExtractProviderName(transaction.Description) 
                        : mapping.Provider;
                    transaction.CategorizationSource = "UserMapping";
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryApplyDefaultPattern(Transaction transaction)
    {
        foreach (var (pattern, category, subcategory, provider) in DefaultPatterns)
        {
            if (Regex.IsMatch(transaction.Description, pattern, RegexOptions.IgnoreCase))
            {
                transaction.Category = category;
                transaction.Subcategory = subcategory;
                transaction.Provider = string.IsNullOrEmpty(provider) 
                    ? ExtractProviderName(transaction.Description) 
                    : provider;
                transaction.CategorizationSource = "Pattern";
                return true;
            }
        }
        return false;
    }

    private async Task ProcessUncategorizedAsync(List<Transaction> uncategorized, bool useAi)
    {
        bool skipAll = false;
        
        // Group similar transactions
        var grouped = uncategorized
            .GroupBy(t => GetDescriptionKey(t.Description))
            .ToList();

        foreach (var group in grouped)
        {
            var sample = group.First();
            string? category = null;
            string? subcategory = null;
            string? provider = null;

            if (skipAll)
            {
                // Apply default uncategorized
                foreach (var transaction in group)
                {
                    transaction.Category = "Uncategorized";
                    transaction.Subcategory = "Other";
                    transaction.Provider = ExtractProviderName(transaction.Description);
                    transaction.CategorizationSource = "Skipped";
                }
                continue;
            }

            // Try AI first
            if (useAi && _aiService != null && _aiService is not FallbackCategorizationService)
            {
                try
                {
                    var result = await _aiService.CategorizeAsync(sample.Description);
                    if (result != null && result.Confidence > 0.7)
                    {
                        category = result.Category;
                        subcategory = result.Subcategory;
                        provider = result.Provider;

                        Console.WriteLine($"\nAI categorized '{sample.Description.Substring(0, Math.Min(50, sample.Description.Length))}...'" +
                                          $"  -> {category}/{subcategory} (Provider: {provider})");
                        Console.Write("Accept? (Y/n/e to edit): ");
                        
                        var response = Console.ReadLine()?.Trim().ToLower();
                        if (response == "n")
                        {
                            category = null; // Will prompt user
                        }
                        else if (response == "e")
                        {
                            var userInput = PromptUserForCategory(sample, ref skipAll);
                            if (userInput.HasValue)
                            {
                                (category, subcategory, provider) = userInput.Value;
                                // Save mapping immediately
                                var pattern = CreatePatternFromDescription(sample.Description);
                                AddAndSaveMapping(pattern, category!, subcategory!, provider ?? "");
                            }
                        }
                        else if (string.IsNullOrEmpty(response) || response == "y")
                        {
                            // AI result accepted - save it as a mapping for future use
                            var pattern = CreatePatternFromDescription(sample.Description);
                            AddAndSaveMapping(pattern, category!, subcategory!, provider ?? "");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AI categorization failed: {ex.Message}");
                }
            }

            // If still not categorized, prompt user
            if (string.IsNullOrEmpty(category) && !skipAll)
            {
                var userInput = PromptUserForCategory(sample, ref skipAll);
                if (userInput.HasValue)
                {
                    (category, subcategory, provider) = userInput.Value;
                    // Save mapping immediately after user input
                    var pattern = CreatePatternFromDescription(sample.Description);
                    AddAndSaveMapping(pattern, category!, subcategory!, provider ?? "");
                }
            }

            // Apply to all transactions in group
            foreach (var transaction in group)
            {
                transaction.Category = category ?? "Uncategorized";
                transaction.Subcategory = subcategory ?? "Other";
                transaction.Provider = provider ?? ExtractProviderName(transaction.Description);
                transaction.CategorizationSource = skipAll ? "Skipped" : "User";
            }
        }
    }

    private (string? category, string? subcategory, string? provider)? PromptUserForCategory(Transaction transaction, ref bool skipAll)
    {
        Console.WriteLine($"\n{'=',-80}");
        Console.WriteLine($"TRANSACTION DETAILS:");
        Console.WriteLine($"{'=',-80}");
        Console.WriteLine($"Description:  {transaction.Description}");
        Console.WriteLine($"Amount:       {transaction.Amount:C2}");
        Console.WriteLine($"Date:         {transaction.TransactionDate:d}");
        Console.WriteLine($"Account:      {transaction.AccountType} - {transaction.AccountId}");
        Console.WriteLine($"Source:       {transaction.SourceFile}");
        Console.WriteLine($"Type:         {transaction.TransactionType}");
        if (transaction.Balance.HasValue)
            Console.WriteLine($"Balance:      {transaction.BalanceDisplay}");
        Console.WriteLine($"{'=',-80}");
        
        Console.WriteLine("\nAvailable categories:");
        for (int i = 0; i < _categoriesConfig.Categories.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {_categoriesConfig.Categories[i].Name}");
        }

        Console.Write("\nEnter category (number or custom name), 's' to skip, 'a' to skip all: ");
        var categoryInput = Console.ReadLine()?.Trim();
        
        if (categoryInput?.ToLower() == "s")
        {
            return null;
        }
        
        if (categoryInput?.ToLower() == "a")
        {
            skipAll = true;
            return null;
        }

        string category;
        CategoryDefinition? selectedCategory = null;

        if (int.TryParse(categoryInput, out int catNum) && catNum >= 1 && catNum <= _categoriesConfig.Categories.Count)
        {
            selectedCategory = _categoriesConfig.Categories[catNum - 1];
            category = selectedCategory.Name;
        }
        else
        {
            category = categoryInput ?? "Other";
            selectedCategory = _categoriesConfig.Categories.FirstOrDefault(c => 
                c.Name.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        // Show existing subcategories for the selected category
        string subcategory;
        if (selectedCategory != null && selectedCategory.Subcategories.Count > 0)
        {
            Console.WriteLine($"\nAvailable subcategories for '{category}':");
            for (int i = 0; i < selectedCategory.Subcategories.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {selectedCategory.Subcategories[i]}");
            }
            Console.Write("\nEnter subcategory (number or custom name): ");
            var subcatInput = Console.ReadLine()?.Trim();

            if (int.TryParse(subcatInput, out int subcatNum) && subcatNum >= 1 && subcatNum <= selectedCategory.Subcategories.Count)
            {
                subcategory = selectedCategory.Subcategories[subcatNum - 1];
            }
            else
            {
                subcategory = subcatInput ?? "Other";
                // Add new subcategory to the category
                if (!string.IsNullOrEmpty(subcategory))
                {
                    _configService.AddSubcategoryIfNeeded(category, subcategory, _configDirectory);
                }
            }
        }
        else
        {
            Console.Write("Enter subcategory: ");
            subcategory = Console.ReadLine()?.Trim() ?? "Other";
            _configService.AddSubcategoryIfNeeded(category, subcategory, _configDirectory);
        }

        Console.Write("Enter provider name (or press Enter to auto-extract): ");
        var provider = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(provider))
            provider = ExtractProviderName(transaction.Description);

        return (category, subcategory, provider);
    }

    private string GetDescriptionKey(string description)
    {
        // Remove numbers and dates to group similar transactions
        var key = Regex.Replace(description, @"\d+", "X");
        key = Regex.Replace(key, @"\d{1,2}/\d{1,2}", "XX/XX");
        key = Regex.Replace(key, @"\s+", " ").Trim();
        
        // Take first significant part
        var parts = key.Split(new[] { ' ', '-', '*' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts.Take(3));
    }

    private string CreatePatternFromDescription(string description)
    {
        // Create a simpler pattern that will match similar transactions
        // Extract the main identifying parts of the description
        var key = description;
        
        // Remove dates like 12/30 or 01/15
        key = Regex.Replace(key, @"\d{1,2}/\d{1,2}(/\d{2,4})?", "");
        
        // Remove transaction IDs and long numbers
        key = Regex.Replace(key, @"\d{6,}", "");
        
        // Remove common suffixes
        key = Regex.Replace(key, @"\s+(PPD ID|WEB ID|transaction#?).*$", "", RegexOptions.IgnoreCase);
        
        // Clean up whitespace
        key = Regex.Replace(key, @"\s+", " ").Trim();
        
        // Take the first meaningful words (usually the merchant name)
        var words = key.Split(new[] { ' ', '*', '-' }, StringSplitOptions.RemoveEmptyEntries)
                       .Where(w => w.Length > 2)
                       .Take(3)
                       .ToArray();
        
        if (words.Length > 0)
        {
            // Escape special regex characters and join
            return string.Join(".*", words.Select(Regex.Escape));
        }
        
        // Fallback: escape the whole thing
        return Regex.Escape(key);
    }

    private void ExtractProviderFromDescription(Transaction transaction)
    {
        transaction.Provider = ExtractProviderName(transaction.Description);
    }

    private string ExtractProviderName(string description)
    {
        // Common patterns to extract provider names
        var patterns = new[]
        {
            @"To\s+([A-Za-z][A-Za-z\s&'-]+?)(?:\s+\d|$)",  // "To PROVIDER NAME"
            @"^([A-Za-z][A-Za-z\s&'-]+?)(?:\s+\d|\*|$)",   // Start of description
            @"([A-Z][A-Za-z&'-]+(?:\s+[A-Z][A-Za-z&'-]+)?)" // Capital words
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(description, pattern);
            if (match.Success && match.Groups[1].Value.Length > 2)
            {
                var provider = match.Groups[1].Value.Trim();
                // Clean up common suffixes
                provider = Regex.Replace(provider, @"\s+(INC|LLC|CORP|CO|LTD)\.?$", "", RegexOptions.IgnoreCase);
                if (provider.Length > 2 && provider.Length < 50)
                    return provider;
            }
        }

        // Fallback: first word(s) of description
        var words = description.Split(new[] { ' ', '*', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return words.FirstOrDefault()?.Trim() ?? "Unknown";
    }

    private string DetermineSubcategory(Transaction transaction)
    {
        // Try to determine subcategory from category and description
        var category = transaction.Category.ToLower();
        var desc = transaction.Description.ToLower();

        return category switch
        {
            "shopping" when desc.Contains("amazon") || desc.Contains("amzn") => "Online Shopping",
            "shopping" when desc.Contains("book") || desc.Contains("judaica") => "Books/Religious Items",
            "groceries" => "Supermarket",
            "gifts & donations" when desc.Contains("yeshiva") => "Religious/Education",
            "gifts & donations" => "Charity",
            "bills & utilities" when desc.Contains("optimum") || desc.Contains("internet") => "Internet/Cable",
            "bills & utilities" when desc.Contains("phone") || desc.Contains("ooma") => "Phone",
            "bills & utilities" => "General",
            "food & drink" => "Restaurant",
            "travel" when desc.Contains("parking") => "Parking",
            "travel" when desc.Contains("gas") || desc.Contains("sunoco") => "Gas",
            "health & wellness" => "Medical",
            "entertainment" => "Recreation",
            "professional services" => "Services",
            "personal" => "Personal Care",
            "gas" => "Fuel",
            _ => "General"
        };
    }
}
