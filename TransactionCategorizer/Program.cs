using TransactionCategorizer.Services;

namespace TransactionCategorizer;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("   Transaction Categorizer & Reporter");
        Console.WriteLine("===========================================\n");

        // Configuration
        var baseDirectory = args.Length > 0 
            ? args[0] 
            : Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) 
              ?? Environment.CurrentDirectory;

        // If running from bin folder, go up to find CSV files
        if (baseDirectory.Contains("bin"))
        {
            baseDirectory = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
        }

        Console.WriteLine($"Working directory: {baseDirectory}\n");

        // Define input files
        var bankAccountFiles = new[]
        {
            Path.Combine(baseDirectory, "Chase1047_Activity_20250304.CSV"),
            Path.Combine(baseDirectory, "Chase4645_Activity_20250304.CSV"),
            Path.Combine(baseDirectory, "Chase6759_Activity_20250304.CSV")
        };

        var creditCardFiles = new[]
        {
            Path.Combine(baseDirectory, "Chase2783_Activity20240101_20241231_20250305.CSV"),
            Path.Combine(baseDirectory, "Chase3383_Activity20240101_20241231_20250305.CSV")
        };

        var mappingsFile = Path.Combine(baseDirectory, "category_mappings.json");
        var outputFile = Path.Combine(baseDirectory, $"Transactions_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

        // Initialize services
        var csvReader = new CsvReaderService();
        
        // Check for OpenAI API key
        IAiCategorizationService? aiService = null;
        var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var azureOpenAiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        var azureOpenAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

        if (!string.IsNullOrEmpty(azureOpenAiKey) && !string.IsNullOrEmpty(azureOpenAiEndpoint))
        {
            Console.WriteLine("Using Azure OpenAI for categorization...");
            var model = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o-mini";
            aiService = new OpenAiCategorizationService(azureOpenAiKey, azureOpenAiEndpoint, model, mappingsFile);
        }
        else if (!string.IsNullOrEmpty(openAiKey))
        {
            Console.WriteLine("Using OpenAI for categorization...");
            aiService = new OpenAiCategorizationService(openAiKey, mappingsFilePath: mappingsFile);
        }
        else
        {
            Console.WriteLine("No AI API key found. Will use pattern matching and user input.");
            Console.WriteLine("Set OPENAI_API_KEY or AZURE_OPENAI_API_KEY/AZURE_OPENAI_ENDPOINT for AI categorization.\n");
            aiService = new FallbackCategorizationService();
        }

        var categorizationService = new CategorizationService(mappingsFile, aiService);
        var excelService = new ExcelExportService();

        // Read all transactions
        var allTransactions = new List<Models.Transaction>();

        Console.WriteLine("Reading bank account files...");
        foreach (var file in bankAccountFiles)
        {
            if (File.Exists(file))
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
                var transactions = csvReader.ReadBankAccountCsv(file);
                allTransactions.AddRange(transactions);
                Console.WriteLine($"    Found {transactions.Count} transactions");
            }
            else
            {
                Console.WriteLine($"  - {Path.GetFileName(file)} (NOT FOUND)");
            }
        }

        Console.WriteLine("\nReading credit card files...");
        foreach (var file in creditCardFiles)
        {
            if (File.Exists(file))
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
                var transactions = csvReader.ReadCreditCardCsv(file);
                allTransactions.AddRange(transactions);
                Console.WriteLine($"    Found {transactions.Count} transactions");
            }
            else
            {
                Console.WriteLine($"  - {Path.GetFileName(file)} (NOT FOUND)");
            }
        }

        Console.WriteLine($"\nTotal transactions loaded: {allTransactions.Count}");

        if (allTransactions.Count == 0)
        {
            Console.WriteLine("No transactions found. Please check the file paths.");
            return;
        }

        // Categorize transactions
        Console.WriteLine("\nCategorizing transactions...");
        Console.WriteLine("(You may be prompted for uncategorized transactions)\n");
        
        await categorizationService.CategorizeTransactionsAsync(allTransactions, useAi: aiService != null);

        // Display summary
        Console.WriteLine("\n===========================================");
        Console.WriteLine("           Categorization Summary");
        Console.WriteLine("===========================================");
        
        var bySource = allTransactions.GroupBy(t => t.CategorizationSource);
        foreach (var group in bySource.OrderBy(g => g.Key))
        {
            Console.WriteLine($"  {group.Key}: {group.Count()} transactions");
        }

        var uncategorized = allTransactions.Count(t => string.IsNullOrEmpty(t.Category) || t.Category == "Uncategorized");
        if (uncategorized > 0)
        {
            Console.WriteLine($"\n  WARNING: {uncategorized} transactions still uncategorized");
        }

        // Export to Excel
        Console.WriteLine("\nGenerating Excel report...");
        excelService.ExportToExcel(allTransactions, outputFile);

        Console.WriteLine("\n===========================================");
        Console.WriteLine("                 Complete!");
        Console.WriteLine("===========================================");
        Console.WriteLine($"\nOutput file: {outputFile}");
        Console.WriteLine($"Mappings saved to: {mappingsFile}");
        
        // Summary statistics
        var credits = allTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var debits = allTransactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
        
        Console.WriteLine($"\nFinancial Summary:");
        Console.WriteLine($"  Total Credits: {credits:C2}");
        Console.WriteLine($"  Total Debits:  {debits:C2}");
        Console.WriteLine($"  Net:           {(credits + debits):C2}");
    }
}
