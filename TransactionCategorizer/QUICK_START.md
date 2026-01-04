# Quick Start Guide

## First Time Setup

1. **Place your CSV files** in a directory (e.g., `C:\Transactions`)

2. **Run the program:**
   ```bash
   TransactionCategorizer.exe
   ```

3. **When prompted for Configuration directory:**
   - Press Enter to use the program's directory, or
   - Enter a custom path where you want to store categories.json and category_mappings.json

4. **When prompted for Data directory:**
   - Enter the path where your CSV files are located
   - All `.CSV` files in that directory will be processed

5. **The program will remember these locations** for next time!

## Subsequent Runs

Just run the program - it will use your saved directory settings:
```bash
TransactionCategorizer.exe
```

## Using Different Datasets

To temporarily use different directories without changing saved settings:
```bash
TransactionCategorizer.exe "C:\MyConfigs" "C:\OtherData"
```

## Categorizing Transactions

When you see an uncategorized transaction:

### Step 1: Review Transaction Details
```
Description:  AMAZON MKTP US*XXXXXXX
Amount:       -$45.99
Date:         1/15/2025
Account:      CreditCard - 3383
```

### Step 2: Select Category
```
Available categories:
  1. Income
  2. Housing
  3. Utilities
  4. Transportation
  5. Groceries
  6. Food & Dining
  7. Shopping
  8. Health & Wellness
  9. Insurance
  10. Gifts & Donations
  11. Financial
  12. Other

Enter: 7
```

### Step 3: Select Subcategory
```
Available subcategories for 'Shopping':
  1. Online
  2. Retail
  3. Office Supplies
  4. Books
  5. Other

Enter: 1
```

### Step 4: Confirm or Enter Provider
```
Enter provider name (or press Enter to auto-extract): 
[Press Enter - it will extract "Amazon"]
```

### That's it! 
The pattern is saved and similar transactions will be automatically categorized in the future.

## Tips

- **Use 's'** to skip a transaction temporarily
- **Use 'a'** to skip all remaining transactions
- **Enter custom names** to create new categories/subcategories
- **Let AI help** by setting up OpenAI or Azure OpenAI API keys
- **Be consistent** - the program learns from your choices

## Environment Variables for AI

### For OpenAI:
```bash
set OPENAI_API_KEY=sk-your-key-here
```

### For Azure OpenAI:
```bash
set AZURE_OPENAI_API_KEY=your-key
set AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
set AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
```

## Output Files

After processing, you'll find:
- **Excel Report**: `Transactions_Report_YYYYMMDD_HHMMSS.xlsx` in your data directory
- **Updated Mappings**: `category_mappings.json` in your config directory
- **Updated Categories**: `categories.json` in your config directory (if you added new subcategories)

## File Locations

### Default Layout:
```
TransactionCategorizer/
??? TransactionCategorizer.exe
??? appsettings.json              (auto-created, stores your directory preferences)
??? categories.json               (category definitions)
??? category_mappings.json        (learned patterns)
??? [your CSV files]              (if using default location)
```

### Custom Layout:
```
C:/
??? MyConfigs/
?   ??? categories.json
?   ??? category_mappings.json
?
??? MyTransactions/
    ??? Chase1047_Activity.CSV
    ??? Chase3383_Activity.CSV
    ??? Transactions_Report_20250115_103000.xlsx
```

## Common Scenarios

### Scenario 1: First Time User
1. Run program
2. When prompted, enter data directory
3. Start categorizing - program learns as you go

### Scenario 2: Regular User
1. Drop new CSV files in your data directory
2. Run program (no prompts needed)
3. Review and categorize only new unknown transactions

### Scenario 3: Multiple Users/Projects
```bash
# Personal finances
TransactionCategorizer.exe "C:\Personal\Config" "C:\Personal\Data"

# Business finances
TransactionCategorizer.exe "C:\Business\Config" "C:\Business\Data"
```

### Scenario 4: One-Time Analysis
1. Run with temporary directories
2. When done, your saved settings remain unchanged

## Customizing Categories

Edit `categories.json` to customize your category structure:

```json
{
  "categories": [
    {
      "name": "Your Custom Category",
      "subcategories": ["Sub1", "Sub2", "Sub3"]
    }
  ]
}
```

Changes take effect immediately on next run.

## Troubleshooting

**Problem**: "Configuration directory not found"  
**Solution**: Let the program create it, or create `categories.json` manually

**Problem**: "No CSV files found"  
**Solution**: Check that files have `.CSV` extension and path is correct

**Problem**: AI not working  
**Solution**: Verify environment variables are set correctly

**Problem**: Want to reset everything  
**Solution**: Delete `appsettings.json` and run program again
