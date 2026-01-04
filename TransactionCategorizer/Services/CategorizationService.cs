using System.Text.Json;
using System.Text.RegularExpressions;
using TransactionCategorizer.Models;

namespace TransactionCategorizer.Services;

public class CategorizationService
{
    private readonly string _mappingsFilePath;
    private CategoryMappings _mappings;
    private readonly IAiCategorizationService? _aiService;

    // Predefined patterns for common transactions
    private static readonly List<(string Pattern, string Category, string Subcategory, string Provider)> DefaultPatterns = new()
    {
        // Utilities
        (@"CONSOLIDATED EDISON|CON ?ED", "Utilities", "Electrical", "Con Edison"),
        (@"NATIONAL GRID|KEYSPAN", "Utilities", "Gas", "National Grid"),
        (@"OPTIMUM", "Utilities", "Internet/Cable", "Optimum"),
        (@"OOMA", "Utilities", "Phone", "Ooma"),
        
        // Banking/Transfers
        (@"ODP TRANSFER|Online Transfer", "Transfers", "Internal Transfer", "Chase"),
        (@"INTEREST PAYMENT", "Income", "Interest", "Chase"),
        (@"CHASE CREDIT CRD AUTOPAY", "Credit Card", "Payment", "Chase"),
        (@"Zelle payment", "Transfers", "Zelle", "Chase"),
        (@"ATM WITHDRAWAL|ATM W/D", "Cash", "ATM Withdrawal", "Chase"),
        (@"WITHDRAWAL \d+/\d+", "Cash", "Withdrawal", "Chase"),
        (@"Payment to Chase card", "Credit Card", "Payment", "Chase"),
        (@"Credit Return", "Refund", "Credit Return", ""),
        
        // Income
        (@"PAYROLL|DIRDEP|Direct Deposit", "Income", "Salary", ""),
        (@"EXTENSIS\s+PAYROLL", "Income", "Salary", "Extensis"),
        (@"Ezer L'Cholim.*DIRDEP", "Income", "Salary", "Ezer L'Cholim"),
        
        // Housing
        (@"TRUIST MORTG|MORTGAGE", "Housing", "Mortgage", "Truist"),
        (@"Prospect Park Ye", "Housing", "Rent/HOA", "Prospect Park"),
        
        // Insurance
        (@"GLICNY|GLIC", "Insurance", "Life Insurance", "Guardian Life"),
        (@"SageSure|SAGESURE", "Insurance", "Home Insurance", "SageSure"),
        (@"NORTHWESTERN MU.*ISA|NORTHWESTERN MUTUAL", "Insurance", "Life Insurance", "Northwestern Mutual"),
        
        // Transportation
        (@"E-Z\*?PASS|EZPASS", "Transportation", "Tolls", "EZ-Pass"),
        (@"SUNOCO|EXXON|SHELL|BP|MOBIL", "Transportation", "Gas", ""),
        (@"NYCDOT PARKING", "Transportation", "Parking", "NYC DOT"),
        
        // Shopping
        (@"AMAZON|AMZN|Amazon", "Shopping", "Online Shopping", "Amazon"),
        (@"Kindle", "Shopping", "Digital Content", "Amazon Kindle"),
        (@"eBay", "Shopping", "Online Shopping", "eBay"),
        (@"STAPLES", "Shopping", "Office Supplies", "Staples"),
        (@"SP STANLEY", "Shopping", "General", "Stanley"),
        
        // Groceries
        (@"MOUNTAIN FRUITS", "Groceries", "Supermarket", "Mountain Fruits"),
        (@"KOSHER PALACE", "Groceries", "Supermarket", "Kosher Palace"),
        (@"BINGO WHOLESALE", "Groceries", "Supermarket", "Bingo Wholesale"),
        (@"TWINS MARKET", "Groceries", "Supermarket", "Twins Market"),
        (@"PRODUCE MARKET", "Groceries", "Supermarket", "Produce Market 2000"),
        (@"OCEAN FRUIT", "Groceries", "Supermarket", "Ocean Fruit"),
        
        // Donations/Charity
        (@"YESHIVA|Yeshiva", "Gifts & Donations", "Religious/Education", ""),
        (@"CHABAD|Chabad", "Gifts & Donations", "Religious", ""),
        (@"HEBRON FUND", "Gifts & Donations", "Charity", "Hebron Fund"),
        (@"Israel Children Cancer", "Gifts & Donations", "Charity", "Israel Children Cancer"),
        (@"CHESEDTODAY", "Gifts & Donations", "Charity", "ChesedToday"),
        (@"YAD L'ACHIM", "Gifts & Donations", "Charity", "Yad L'Achim"),
        (@"Chofetz Chaim", "Gifts & Donations", "Religious", "Chofetz Chaim Heritage"),
        (@"Feed Israel", "Gifts & Donations", "Charity", "Feed Israel"),
        (@"YOUNG ISRAEL|Young Israel", "Gifts & Donations", "Religious", "Young Israel"),
        (@"Congregation|CONGREGATION", "Gifts & Donations", "Religious", ""),
        (@"GoFundMe", "Gifts & Donations", "Charity", "GoFundMe"),
        (@"TikvaChildrensHo", "Gifts & Donations", "Charity", "Tikva Children's Home"),
        (@"Wikimedia", "Gifts & Donations", "Non-Profit", "Wikimedia"),
        
        // Food & Dining
        (@"BRICK OVEN|PIZZA|RESTAURANT", "Food & Drink", "Restaurant", ""),
        (@"THE CIRCLE", "Food & Drink", "Restaurant", "The Circle"),
        (@"BENNY.*BRICK OVEN", "Food & Drink", "Restaurant", "Benny's Brick Oven"),
        
        // Services
        (@"MONEYGRAM", "Services", "Money Transfer", "MoneyGram"),
        (@"LAUNDRY", "Services", "Laundry", ""),
        (@"HILA", "Services", "Professional Services", "Hila"),
        (@"CHECK \d+", "Services", "Check Payment", ""),
        
        // Health
        (@"PHARMACY|HEALTH|VOORHIES", "Health & Wellness", "Pharmacy", ""),
        
        // Entertainment
        (@"J LEAGUES", "Entertainment", "Sports/Recreation", "J Leagues"),
        (@"GOOGLE.*YouTube|YouTube", "Entertainment", "Streaming", "YouTube"),
        
        // Books/Education
        (@"BERMAN BOOKS|JUDAICA|MEKOR", "Shopping", "Books/Religious Items", ""),
        
        // Bills
        (@"BILLPAY|Bill Pay|Online Payment.*To", "Bills & Utilities", "Bill Payment", ""),
        
        // Credit Card specific
        (@"AUTOMATIC PAYMENT.*THANK", "Credit Card", "Payment", "Chase"),
        
        // Loans
        (@"DEPT EDUCATION|STUDENT LN|STUDENT LOAN", "Loans", "Education Loan", "Dept of Education"),
        (@"LOAN_PMT|LOAN PMT", "Loans", "Loan Payment", ""),
        
        // Gas (category from credit card)
        (@"Gas", "Transportation", "Gas", ""),
        
        // Travel
        (@"Travel", "Transportation", "Travel", ""),
    };

    public CategorizationService(string mappingsFilePath, IAiCategorizationService? aiService = null)
    {
        _mappingsFilePath = mappingsFilePath;
        _aiService = aiService;
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
        Console.WriteLine($"\n{'=',-60}");
        Console.WriteLine($"Transaction: {transaction.Description}");
        Console.WriteLine($"Amount: {transaction.Amount:C2}");
        Console.WriteLine($"Date: {transaction.TransactionDate:d}");
        Console.WriteLine($"Account: {transaction.AccountType} - {transaction.AccountId}");
        Console.WriteLine($"{'=',-60}");
        
        Console.WriteLine("\nCommon categories:");
        var categories = new[] 
        { 
            "1. Utilities", "2. Transportation", "3. Groceries", "4. Shopping", 
            "5. Food & Drink", "6. Gifts & Donations", "7. Income", "8. Transfers",
            "9. Housing", "10. Insurance", "11. Health & Wellness", "12. Entertainment",
            "13. Services", "14. Bills & Utilities", "15. Credit Card", "16. Loans", "17. Other"
        };
        
        foreach (var cat in categories)
            Console.WriteLine($"  {cat}");

        Console.Write("\nEnter category (number/custom), 's' to skip, 'a' to skip all: ");
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
        if (int.TryParse(categoryInput, out int catNum) && catNum >= 1 && catNum <= 17)
        {
            category = categories[catNum - 1].Substring(categories[catNum - 1].IndexOf(' ') + 1);
        }
        else
        {
            category = categoryInput ?? "Other";
        }

        Console.Write("Enter subcategory: ");
        var subcategory = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(subcategory))
            subcategory = "Other";

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
