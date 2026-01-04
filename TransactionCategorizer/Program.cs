using TransactionCategorizer.Services;

namespace TransactionCategorizer;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("   Transaction Categorizer & Reporter");
        Console.WriteLine("===========================================\n");

        // Initialize configuration service
        var configService = new ConfigurationService();
        
        // Get directories from command-line args or prompt user
        var configDirectory = configService.GetConfigDirectory(args.Length > 0 ? args[0] : null);
        var dataDirectory = configService.GetDataDirectory(args.Length > 1 ? args[1] : null);

        Console.WriteLine($"Configuration directory: {configDirectory}");
        Console.WriteLine($"Data directory: {dataDirectory}\n");

        // Define paths for config files
        var mappingsFile = Path.Combine(configDirectory, "category_mappings.json");
        var outputFile = Path.Combine(dataDirectory, $"Transactions_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

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
            aiService = new OpenAiCategorizationService(azureOpenAiKey, configService, configDirectory, azureOpenAiEndpoint, model, mappingsFile);
        }
        else if (!string.IsNullOrEmpty(openAiKey))
        {
            Console.WriteLine("Using OpenAI for categorization...");
            aiService = new OpenAiCategorizationService(openAiKey, configService, configDirectory, mappingsFilePath: mappingsFile);
        }
        else
        {
            Console.WriteLine("No AI API key found. Will use pattern matching and user input.");
            Console.WriteLine("Set OPENAI_API_KEY or AZURE_OPENAI_API_KEY/AZURE_OPENAI_ENDPOINT for AI categorization.\n");
            aiService = new FallbackCategorizationService();
        }

        var categorizationService = new CategorizationService(mappingsFile, configService, configDirectory, aiService);
        var excelService = new ExcelExportService();

        // Find all CSV files in the data directory
        var csvFiles = Directory.GetFiles(dataDirectory, "*.CSV", SearchOption.TopDirectoryOnly);
        
        if (csvFiles.Length == 0)
        {
            Console.WriteLine($"No CSV files found in {dataDirectory}");
            return;
        }

        // Read all transactions
        var allTransactions = new List<Models.Transaction>();

        Console.WriteLine("Reading transaction files...");
        foreach (var file in csvFiles)
        {
            Console.WriteLine($"  - {Path.GetFileName(file)}");
            
            try
            {
                // Determine file type by checking headers or content
                var firstLine = File.ReadLines(file).FirstOrDefault() ?? "";
                
                List<Models.Transaction> transactions;
                if (firstLine.Contains("Posting Date") && firstLine.Contains("Balance"))
                {
                    // Bank account format
                    transactions = csvReader.ReadBankAccountCsv(file);
                }
                else if (firstLine.Contains("Transaction Date") && firstLine.Contains("Post Date"))
                {
                    // Credit card format
                    transactions = csvReader.ReadCreditCardCsv(file);
                }
                else
                {
                    Console.WriteLine($"    Unknown format, skipping...");
                    continue;
                }
                
                allTransactions.AddRange(transactions);
                Console.WriteLine($"    Found {transactions.Count} transactions");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Error reading file: {ex.Message}");
            }
        }

        Console.WriteLine($"\nTotal transactions loaded: {allTransactions.Count}");

        if (allTransactions.Count == 0)
        {
            Console.WriteLine("No transactions found. Please check the files.");
            return;
        }

        // Categorize transactions
        Console.WriteLine("\nCategorizing transactions...");
        Console.WriteLine("(You may be prompted for uncategorized transactions)\n");
        
        await categorizationService.CategorizeTransactionsAsync(allTransactions, useAi: aiService != null && aiService is not FallbackCategorizationService);

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
