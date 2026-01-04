using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using TransactionCategorizer.Models;

namespace TransactionCategorizer.Services;

public class CsvReaderService
{
    public List<Transaction> ReadBankAccountCsv(string filePath)
    {
        var transactions = new List<Transaction>();
        var accountId = ExtractAccountId(filePath);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        
        // Register class map
        csv.Context.RegisterClassMap<BankTransactionMap>();
        
        var records = csv.GetRecords<BankTransaction>().ToList();

        foreach (var record in records)
        {
            if (!DateTime.TryParse(record.PostingDate, out var postingDate))
                continue;

            transactions.Add(new Transaction
            {
                SourceFile = Path.GetFileName(filePath),
                AccountType = "Bank",
                AccountId = accountId,
                TransactionDate = postingDate,
                PostingDate = postingDate,
                Description = record.Description.Trim('"'),
                Amount = record.Amount,
                OriginalType = record.Type,
                Balance = record.Balance,
                CheckNumber = record.CheckOrSlipNumber
            });
        }

        return transactions;
    }

    public List<Transaction> ReadCreditCardCsv(string filePath)
    {
        var transactions = new List<Transaction>();
        var accountId = ExtractAccountId(filePath);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        
        csv.Context.RegisterClassMap<CreditCardTransactionMap>();
        
        var records = csv.GetRecords<CreditCardTransaction>().ToList();

        foreach (var record in records)
        {
            if (!DateTime.TryParse(record.TransactionDate, out var transactionDate))
                continue;
            
            DateTime.TryParse(record.PostDate, out var postDate);

            transactions.Add(new Transaction
            {
                SourceFile = Path.GetFileName(filePath),
                AccountType = "CreditCard",
                AccountId = accountId,
                TransactionDate = transactionDate,
                PostingDate = postDate != default ? postDate : transactionDate,
                Description = record.Description,
                Amount = record.Amount,
                OriginalType = record.Type,
                Category = record.Category ?? string.Empty,
                CategorizationSource = !string.IsNullOrEmpty(record.Category) ? "Original" : string.Empty
            });
        }

        return transactions;
    }

    private string ExtractAccountId(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        // Extract account number from filename like "Chase1047_Activity_..." or "Chase3383_Activity..."
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"Chase(\d+)_");
        return match.Success ? match.Groups[1].Value : "Unknown";
    }
}

public sealed class BankTransactionMap : ClassMap<BankTransaction>
{
    public BankTransactionMap()
    {
        Map(m => m.Details).Name("Details");
        Map(m => m.PostingDate).Name("Posting Date");
        Map(m => m.Description).Name("Description");
        Map(m => m.Amount).Name("Amount");
        Map(m => m.Type).Name("Type");
        Map(m => m.Balance).Name("Balance").Optional();
        Map(m => m.CheckOrSlipNumber).Name("Check or Slip #").Optional();
    }
}

public sealed class CreditCardTransactionMap : ClassMap<CreditCardTransaction>
{
    public CreditCardTransactionMap()
    {
        Map(m => m.TransactionDate).Name("Transaction Date");
        Map(m => m.PostDate).Name("Post Date");
        Map(m => m.Description).Name("Description");
        Map(m => m.Category).Name("Category").Optional();
        Map(m => m.Type).Name("Type");
        Map(m => m.Amount).Name("Amount");
        Map(m => m.Memo).Name("Memo").Optional();
    }
}
