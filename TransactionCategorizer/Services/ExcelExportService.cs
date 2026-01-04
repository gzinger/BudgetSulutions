using ClosedXML.Excel;
using TransactionCategorizer.Models;

namespace TransactionCategorizer.Services;

public class ExcelExportService
{
    public void ExportToExcel(List<Transaction> transactions, string outputPath)
    {
        using var workbook = new XLWorkbook();
        
        // Create summary sheet
        CreateSummarySheet(workbook, transactions);
        
        // Create detailed sheets by Credit/Debit
        CreateDetailedSheet(workbook, transactions.Where(t => t.TransactionType == "Credit").ToList(), "Credits");
        CreateDetailedSheet(workbook, transactions.Where(t => t.TransactionType == "Debit").ToList(), "Debits");
        
        // Create all transactions sheet
        CreateAllTransactionsSheet(workbook, transactions);
        
        workbook.SaveAs(outputPath);
        Console.WriteLine($"\nExcel file saved to: {outputPath}");
    }

    private void CreateSummarySheet(IXLWorkbook workbook, List<Transaction> transactions)
    {
        var ws = workbook.Worksheets.Add("Summary");
        var row = 1;

        // Title
        ws.Cell(row, 1).Value = "Transaction Summary Report";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        ws.Range(row, 1, row, 4).Merge();
        row += 2;

        // Date range
        var minDate = transactions.Min(t => t.TransactionDate);
        var maxDate = transactions.Max(t => t.TransactionDate);
        ws.Cell(row, 1).Value = $"Period: {minDate:d} - {maxDate:d}";
        row += 2;

        // Overall totals
        ws.Cell(row, 1).Value = "Overall Summary";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        var totalCredits = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var totalDebits = transactions.Where(t => t.Amount < 0).Sum(t => t.Amount);

        ws.Cell(row, 1).Value = "Total Credits:";
        ws.Cell(row, 2).Value = totalCredits;
        ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, 2).Style.Font.FontColor = XLColor.Green;
        row++;

        ws.Cell(row, 1).Value = "Total Debits:";
        ws.Cell(row, 2).Value = totalDebits;
        ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
        row++;

        ws.Cell(row, 1).Value = "Net:";
        ws.Cell(row, 2).Value = totalCredits + totalDebits;
        ws.Cell(row, 2).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.Bold = true;
        row += 2;

        // Summary by Category for Debits (expenses)
        ws.Cell(row, 1).Value = "Expenses by Category";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        var headers = new[] { "Category", "Subcategory", "Count", "Total" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        var debitsByCategory = transactions
            .Where(t => t.Amount < 0)
            .GroupBy(t => new { t.Category, t.Subcategory })
            .OrderBy(g => g.Key.Category)
            .ThenBy(g => g.Key.Subcategory)
            .ToList();

        string lastCategory = "";
        foreach (var group in debitsByCategory)
        {
            if (lastCategory != group.Key.Category)
            {
                // Category total row
                if (!string.IsNullOrEmpty(lastCategory))
                {
                    var categoryTotal = debitsByCategory
                        .Where(g => g.Key.Category == lastCategory)
                        .Sum(g => g.Sum(t => t.Amount));
                    
                    ws.Cell(row, 1).Value = $"{lastCategory} Total";
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 4).Value = categoryTotal;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                    ws.Cell(row, 4).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    row++;
                }
                lastCategory = group.Key.Category;
            }

            ws.Cell(row, 1).Value = group.Key.Category;
            ws.Cell(row, 2).Value = group.Key.Subcategory;
            ws.Cell(row, 3).Value = group.Count();
            ws.Cell(row, 4).Value = group.Sum(t => t.Amount);
            ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            row++;
        }

        // Last category total
        if (!string.IsNullOrEmpty(lastCategory))
        {
            var categoryTotal = debitsByCategory
                .Where(g => g.Key.Category == lastCategory)
                .Sum(g => g.Sum(t => t.Amount));
            
            ws.Cell(row, 1).Value = $"{lastCategory} Total";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 4).Value = categoryTotal;
            ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightYellow;
            row++;
        }

        // Grand total for debits
        row++;
        ws.Cell(row, 1).Value = "TOTAL EXPENSES";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = totalDebits;
        ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightBlue;

        ws.Columns().AdjustToContents();
    }

    private void CreateDetailedSheet(IXLWorkbook workbook, List<Transaction> transactions, string sheetName)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        var row = 1;

        // Group by Category, then Subcategory, sort by Provider, then Date
        var grouped = transactions
            .GroupBy(t => t.Category)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var categoryGroup in grouped)
        {
            // Category header
            ws.Cell(row, 1).Value = categoryGroup.Key;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            ws.Range(row, 1, row, 8).Merge();
            row++;

            var subGroups = categoryGroup
                .GroupBy(t => t.Subcategory)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var subGroup in subGroups)
            {
                // Subcategory header
                ws.Cell(row, 1).Value = $"  {subGroup.Key}";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 12;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Range(row, 1, row, 8).Merge();
                row++;

                // Column headers
                var headers = new[] { "Date", "Provider", "Description", "Amount", "Account", "Type", "Source File", "Categorization" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = headers[i];
                    ws.Cell(row, i + 1).Style.Font.Bold = true;
                    ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.WhiteSmoke;
                }
                row++;

                // Transactions sorted by Provider, then Date
                var sortedTransactions = subGroup
                    .OrderBy(t => t.Provider)
                    .ThenBy(t => t.TransactionDate)
                    .ToList();

                foreach (var t in sortedTransactions)
                {
                    ws.Cell(row, 1).Value = t.TransactionDate;
                    ws.Cell(row, 1).Style.NumberFormat.Format = "MM/dd/yyyy";
                    ws.Cell(row, 2).Value = t.Provider;
                    ws.Cell(row, 3).Value = t.Description;
                    ws.Cell(row, 4).Value = t.Amount;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                    if (t.Amount < 0)
                        ws.Cell(row, 4).Style.Font.FontColor = XLColor.Red;
                    else
                        ws.Cell(row, 4).Style.Font.FontColor = XLColor.Green;
                    ws.Cell(row, 5).Value = $"{t.AccountType}-{t.AccountId}";
                    ws.Cell(row, 6).Value = t.OriginalType;
                    ws.Cell(row, 7).Value = t.SourceFile;
                    ws.Cell(row, 8).Value = t.CategorizationSource;
                    row++;
                }

                // Subcategory total
                ws.Cell(row, 3).Value = $"{subGroup.Key} Total:";
                ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 4).Value = subGroup.Sum(t => t.Amount);
                ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Style.Font.Bold = true;
                ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightYellow;
                row++;
                row++; // Empty row
            }

            // Category total
            ws.Cell(row, 3).Value = $"{categoryGroup.Key} TOTAL:";
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 4).Value = categoryGroup.Sum(t => t.Amount);
            ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightGreen;
            row++;
            row++; // Empty row between categories
        }

        // Grand total
        ws.Cell(row, 3).Value = $"GRAND TOTAL ({sheetName}):";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 4).Value = transactions.Sum(t => t.Amount);
        ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0.00";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightBlue;

        ws.Columns().AdjustToContents();
        ws.Column(3).Width = 60; // Description column wider
    }

    private void CreateAllTransactionsSheet(IXLWorkbook workbook, List<Transaction> transactions)
    {
        var ws = workbook.Worksheets.Add("All Transactions");
        
        // Headers
        var headers = new[] { "Date", "Type", "Category", "Subcategory", "Provider", "Description", "Amount", "Account", "Original Type", "Source", "Categorization" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = 2;
        var sorted = transactions
            .OrderBy(t => t.TransactionType)
            .ThenBy(t => t.Category)
            .ThenBy(t => t.Subcategory)
            .ThenBy(t => t.Provider)
            .ThenBy(t => t.TransactionDate)
            .ToList();

        foreach (var t in sorted)
        {
            ws.Cell(row, 1).Value = t.TransactionDate;
            ws.Cell(row, 1).Style.NumberFormat.Format = "MM/dd/yyyy";
            ws.Cell(row, 2).Value = t.TransactionType;
            ws.Cell(row, 3).Value = t.Category;
            ws.Cell(row, 4).Value = t.Subcategory;
            ws.Cell(row, 5).Value = t.Provider;
            ws.Cell(row, 6).Value = t.Description;
            ws.Cell(row, 7).Value = t.Amount;
            ws.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
            if (t.Amount < 0)
                ws.Cell(row, 7).Style.Font.FontColor = XLColor.Red;
            else
                ws.Cell(row, 7).Style.Font.FontColor = XLColor.Green;
            ws.Cell(row, 8).Value = $"{t.AccountType}-{t.AccountId}";
            ws.Cell(row, 9).Value = t.OriginalType;
            ws.Cell(row, 10).Value = t.SourceFile;
            ws.Cell(row, 11).Value = t.CategorizationSource;
            row++;
        }

        // Add filters and auto-fit
        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();
        ws.Column(6).Width = 60; // Description column
    }
}
