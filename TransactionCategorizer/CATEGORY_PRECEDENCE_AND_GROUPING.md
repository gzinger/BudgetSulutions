# Changes Summary - Category Precedence and Excel Grouping

## Overview
Two key improvements were implemented to enhance the transaction categorization workflow and Excel reporting.

## Change 1: Category Mappings Take Precedence

### Problem
Previously, categories from the input CSV files (credit card statements) would override user-defined patterns in `category_mappings.json`. This meant that if a user manually categorized a transaction differently than what the credit card company categorized it, the user's choice would be lost on the next run.

### Solution
Reordered the categorization logic so that `category_mappings.json` patterns are checked **first**, before the source file categories.

### Updated Flow
**Before:**
1. Check if category from source file (CSV)
2. Try user mappings from category_mappings.json
3. Try default patterns
4. AI or manual input

**After:**
1. ? **Try user mappings from category_mappings.json FIRST**
2. Check if category from source file (CSV)
3. Try default patterns
4. AI or manual input

### Code Changes
**File:** `TransactionCategorizer/Services/CategorizationService.cs`

**Method:** `CategorizeTransactionsAsync`

```csharp
public async Task CategorizeTransactionsAsync(List<Transaction> transactions, bool useAi = true)
{
    var uncategorized = new List<Transaction>();

    foreach (var transaction in transactions)
    {
        // Try user mappings FIRST - they take precedence over everything including source file
        if (TryApplyUserMapping(transaction))
            continue;

        // Then check if already categorized from source file
        if (!string.IsNullOrEmpty(transaction.Category) && transaction.CategorizationSource == "Original")
        {
            ExtractProviderFromDescription(transaction);
            if (string.IsNullOrEmpty(transaction.Subcategory))
            {
                transaction.Subcategory = DetermineSubcategory(transaction);
            }
            continue;
        }

        // Try default patterns
        if (TryApplyDefaultPattern(transaction))
            continue;

        uncategorized.Add(transaction);
    }
    // ...
}
```

### Benefits
- **User preferences respected**: Manual categorizations are never overridden
- **Learning system**: Once you categorize a transaction, it stays categorized your way
- **Flexibility**: You can override credit card company categories that don't match your budget structure
- **Consistency**: Same merchants always categorized the same way regardless of source

### Example
```
Transaction: "STARBUCKS #12345"
Credit Card CSV Category: "Food & Dining"
Your category_mappings.json: "Other" / "Entertainment"

Result: Uses YOUR categorization ("Other"/"Entertainment")
```

---

## Change 2: Excel Grouping/Outlining with Collapse/Expand

### Problem
Excel reports showed all transactions expanded, making it difficult to get a quick overview of spending by category without scrolling through hundreds of individual transactions.

### Solution
Implemented Excel row grouping (outlining) with **smart sorting** so that:
- Categories are sorted by total spending (highest to lowest)
- Subcategories within each category are also sorted by total (highest to lowest)
- Individual transactions remain sorted by Provider, then Date
- All groups can be collapsed to show only totals

### Sorting Logic

**Three-level sorting hierarchy:**
1. **Categories**: Sorted by total amount (descending by absolute value)
2. **Subcategories**: Sorted by total amount (descending by absolute value)
3. **Transactions**: Sorted by Provider, then Date (for consistency)

This means your highest spending categories appear at the top, making it easy to see where most money is going.

### Implementation Details

#### Summary Sheet
- Categories sorted by total expenses (highest first)
- Subcategories within each category sorted by total (highest first)
- Click the `[-]` button to collapse subcategories
- Click the `[+]` button to expand them
- Shows category totals when collapsed

#### Credits/Debits Sheets (Detailed)
- **Three-level sorting:**
  1. Categories by total (highest spending first)
  2. Subcategories by total within category (highest first)
  3. Transactions by Provider, then Date
- **Two-level grouping:**
  1. **Level 1**: Individual transactions grouped under subcategory
  2. **Level 2**: Subcategories grouped under category
- All groups start collapsed for clean overview
- Expand as needed to see details

### Visual Structure

```
Collapsed View (sorted by total):
  [+] Food & Dining                     -$1,567.89  ? Highest spending
  [+] Shopping                          -$1,234.56
  [+] Transportation                      -$345.67

Partially Expanded (Shopping):
  [-] Shopping                          -$1,234.56
      [+] Online                          -$800.00  ? Highest subcategory
      [+] Retail                          -$234.56
      [+] Books                           -$200.00

Fully Expanded (Shopping > Online):
  [-] Shopping                          -$1,234.56
      [-] Online                          -$800.00
          Date    Provider    Desc        Amount
          01/15   Amazon      AMZN...     -$45.99  ? Sorted by Provider
          01/16   Amazon      AMZN...     -$23.50
          01/20   eBay        EBAY...    -$150.00
          ...
          Online Total:                   -$800.00
      [+] Retail                          -$234.56
      [+] Books                           -$200.00
```

### Code Changes
**File:** `TransactionCategorizer/Services/ExcelExportService.cs`

**Method:** `CreateDetailedSheet`

```csharp
// Group and sort by total (descending by absolute value)
var grouped = transactions
    .GroupBy(t => t.Category)
    .Select(cg => new
    {
        Category = cg.Key,
        Total = cg.Sum(t => t.Amount),
        Subcategories = cg
            .GroupBy(t => t.Subcategory)
            .Select(sg => new
            {
                Subcategory = sg.Key,
                Total = sg.Sum(t => t.Amount),
                Transactions = sg
                    .OrderBy(t => t.Provider)
                    .ThenBy(t => t.TransactionDate)
                    .ToList()
            })
            .OrderByDescending(sg => Math.Abs(sg.Total))  // Sort subcategories
            .ToList()
    })
    .OrderByDescending(cg => Math.Abs(cg.Total))  // Sort categories
    .ToList();

// Group rows for collapse/expand
ws.Rows(transactionStartRow, row - 2).Group();
ws.Rows(transactionStartRow, row - 2).Collapse();
ws.Rows(categoryStartRow + 1, row - 2).Group();
ws.Rows(categoryStartRow + 1, row - 2).Collapse();
```

**Method:** `CreateSummarySheet`

```csharp
// Sort by category total, then by subcategory total
var categoryGroups = debitsByCategory
    .GroupBy(g => g.Key.Category)
    .Select(cg => new
    {
        Category = cg.Key,
        Total = cg.Sum(g => g.Sum(t => t.Amount)),
        Subcategories = cg
            .Select(g => new { ... })
            .OrderByDescending(sg => Math.Abs(sg.Total))
            .ToList()
    })
    .OrderByDescending(cg => Math.Abs(cg.Total))
    .ToList();
```

### Features
- **Outline buttons**: `[+]` and `[-]` buttons appear in the left margin
- **Level indicators**: Numbers (1, 2) show grouping depth
- **Quick collapse/expand all**: Use Excel's outline level buttons (1, 2, 3)
- **Preserves totals**: Category and subcategory totals always visible
- **Smart sorting**: Highest spending appears first
- **Professional appearance**: Clean, organized reports

### Benefits
- **Instant insights**: See your top spending categories immediately
- **Quick overview**: See spending by category at a glance
- **Drill-down analysis**: Expand only the categories you want to examine
- **Priority focus**: Highest spending items appear first
- **Better budgeting**: Easy to identify areas to cut back
- **Print-friendly**: Collapse all for summary reports
- **Better decision making**: Easy to spot spending patterns
- **Customizable view**: Each user can expand/collapse as needed

### Excel User Interface

When you open the Excel file, you'll see:
```
  1  2  3
  ? [-] Food & Dining                                          -$1,567.89
  ? [+] Shopping                                               -$1,234.56
```

Where:
- **1, 2, 3** = Click to collapse/expand to that level
- **? ?** = Outline symbols (appear when hovering)
- **[+]** = Expand this group
- **[-]** = Collapse this group
- **Categories are sorted by total** (highest spending first)

### Usage Tips
1. **See totals only**: Click the `1` button at top to show only categories (sorted by spending)
2. **See subcategories**: Click the `2` button to show categories and subcategories (both sorted by total)
3. **See all details**: Click the `3` button to show individual transactions
4. **Custom view**: Click individual `[+]`/`[-]` to expand/collapse specific sections
5. **Find top spenders**: The first few collapsed categories show where most money goes

---

## Testing Recommendations

### Change 1: Category Precedence
1. Create a mapping in `category_mappings.json` for a known merchant
2. Import a CSV that has the same merchant with a different category
3. Verify the mapping from `category_mappings.json` is used
4. Check the `CategorizationSource` column shows "UserMapping"

### Change 2: Excel Grouping and Sorting
1. Generate an Excel report with transactions in multiple categories
2. Open the Credits or Debits sheet
3. Verify categories are sorted by total (highest first)
4. Expand a category and verify subcategories are sorted by total
5. Expand a subcategory and verify transactions are sorted by Provider, then Date
6. Click `[+]` buttons to expand categories
7. Click `[-]` buttons to collapse
8. Test the level buttons (1, 2, 3) at the top
9. Verify totals are correct when collapsed
10. Confirm highest spending category is at the top

---

## Impact on Existing Users

### Change 1
- **Backward compatible**: Existing `category_mappings.json` files work without modification
- **Immediate benefit**: User categorizations now persist correctly
- **No action required**: Change is automatic and transparent

### Change 2
- **Backward compatible**: Excel files still open in older Excel versions
- **Enhanced experience**: Modern Excel shows grouping controls with smart sorting
- **No action required**: Grouping and sorting are automatic
- **User choice**: Can ignore grouping and work with all rows expanded
- **Better insights**: Immediately see top spending areas

---

## Files Modified

1. **TransactionCategorizer/Services/CategorizationService.cs**
   - Reordered categorization logic
   - User mappings now checked first

2. **TransactionCategorizer/Services/ExcelExportService.cs**
   - Added row grouping to `CreateDetailedSheet`
   - Added row grouping to `CreateSummarySheet`
   - Implemented sorting by totals (categories and subcategories)
   - Preserved Provider/Date sorting within subcategories
   - Set outline visibility properties

---

## Future Enhancements

### Category Precedence
- Add UI to view/edit precedence order
- Show which rule matched each transaction
- Allow temporary override per transaction

### Excel Grouping and Sorting
- Add grouping to "All Transactions" sheet
- Option to export with groups expanded
- Configurable default collapse level
- Save user's preferred collapse state
- Option to sort ascending or descending
- Multiple sort options (by date, by total, by count, etc.)

---

## Summary

These changes significantly improve the user experience:

1. **Category Mappings Precedence**: Ensures your categorization choices are always respected
2. **Excel Grouping with Smart Sorting**: Makes reports more readable with highest spending items first, enabling better analysis and budget insights

Both changes are backward compatible and require no user action to benefit from them. The smart sorting feature particularly helps with budget analysis by immediately highlighting where the most money is being spent.
