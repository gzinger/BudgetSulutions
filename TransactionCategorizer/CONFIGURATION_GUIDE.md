# Transaction Categorizer - Configuration Guide

## Overview
The Transaction Categorizer now uses a flexible configuration system that allows you to customize categories, manage file locations, and learn from your categorization patterns.

## Configuration Files

### 1. `appsettings.json`
Stores the locations of configuration and data directories:
```json
{
  "configDirectory": "",
  "dataDirectory": ""
}
```

- **configDirectory**: Where `categories.json` and `category_mappings.json` are located
- **dataDirectory**: Where your CSV transaction files are located

### 2. `categories.json`
Defines your category hierarchy with up to 12 main categories and up to 5 subcategories each:
```json
{
  "categories": [
    {
      "name": "Income",
      "subcategories": ["Salary", "Interest", "Refund", "Other"],
      "inferenceRules": [
        {
          "subcategory": "Salary",
          "keywords": ["payroll", "salary", "wages"]
        },
        {
          "subcategory": "Interest",
          "keywords": ["interest"]
        }
      ]
    },
    ...
  ]
}
```

**Inference Rules**: Each category can have optional inference rules that automatically determine the subcategory based on keywords in the transaction description. This is used when transactions have a category but no subcategory (common with credit card CSV files).

For example, if a transaction has category "Income" but no subcategory, and the description contains "payroll", it will automatically be assigned the "Salary" subcategory.

The default categories are:
1. **Income** - Salary, Interest, Refund, Other
2. **Housing** - Mortgage, Rent, HOA, Repairs, Other
3. **Utilities** - Electric, Gas, Water, Internet/Cable, Phone
4. **Transportation** - Gas/Fuel, Tolls, Parking, Public Transit, Other
5. **Groceries** - Supermarket, Specialty, Wholesale, Other
6. **Food & Dining** - Restaurant, Fast Food, Takeout, Coffee, Other
7. **Shopping** - Online, Retail, Office Supplies, Books, Other
8. **Health & Wellness** - Pharmacy, Medical, Dental, Fitness, Other
9. **Insurance** - Life, Home, Auto, Health, Other
10. **Gifts & Donations** - Religious, Charity, Education, Gifts, Other
11. **Financial** - Transfer, Payment, Fee, ATM, Other
12. **Other** - Entertainment, Services, Personal, Miscellaneous, Uncategorized

### 3. `category_mappings.json`
Automatically created and updated as you categorize transactions. Stores patterns learned from your manual categorizations:
```json
{
  "mappings": [
    {
      "pattern": "AMAZON",
      "category": "Shopping",
      "subcategory": "Online",
      "provider": "Amazon"
    }
  ],
  "lastUpdated": "2025-01-15T10:30:00"
}
```

## Command Line Usage

### Basic Usage
```bash
TransactionCategorizer.exe
```
If directories are not configured, you will be prompted to enter them.

### Specify Configuration Directory
```bash
TransactionCategorizer.exe "C:\MyConfigs"
```
This directory should contain `categories.json` and `category_mappings.json`.

### Specify Both Directories
```bash
TransactionCategorizer.exe "C:\MyConfigs" "C:\MyTransactionData"
```
- First argument: Configuration directory
- Second argument: Data directory (containing CSV files)

## Directory Location Priority

The application looks for directories in this order:

1. **Command-line arguments** (highest priority)
2. **Saved settings** in `appsettings.json`
3. **Program directory** (checks for files in the same folder as the executable)
4. **User prompt** (if not found, asks you to specify)

Once you specify directories, they are saved in `appsettings.json` for future use.

## Transaction Categorization Flow

1. **Original Category**: If a transaction already has a category from the CSV file (credit cards), it's preserved
2. **User Mappings**: Checks patterns you've previously defined in `category_mappings.json`
3. **Default Patterns**: Uses built-in patterns for common merchants
4. **AI Categorization**: If configured, uses OpenAI/Azure OpenAI to suggest categories
5. **Manual Input**: Prompts you to categorize unknown transactions

## Manual Categorization Interface

When prompted to categorize a transaction, you'll see:

```
================================================================================
TRANSACTION DETAILS:
================================================================================
Description:  AMAZON MKTP US*XXXXXXX
Amount:       -$45.99
Date:         1/15/2025
Account:      CreditCard - 3383
Source:       Chase3383_Activity20240101_20241231_20250305.CSV
Type:         Debit
Balance:      $1,234.56
================================================================================

Available categories:
  1. Income
  2. Housing
  3. Utilities
  ... (all categories listed)

Enter category (number or custom name), 's' to skip, 'a' to skip all: 
```

### Options:
- **Enter a number** (e.g., `7`) to select a predefined category
- **Enter a custom name** to create a new category
- **'s'** to skip this transaction
- **'a'** to skip all remaining uncategorized transactions

### Subcategory Selection:
After selecting a category, you'll see existing subcategories:

```
Available subcategories for 'Shopping':
  1. Online
  2. Retail
  3. Office Supplies
  4. Books
  5. Other

Enter subcategory (number or custom name):
```

- **Enter a number** to select an existing subcategory
- **Enter a custom name** to create a new subcategory (automatically saved to `categories.json`)

## AI Configuration

### OpenAI
Set environment variable:
```bash
set OPENAI_API_KEY=sk-...
```

### Azure OpenAI
Set environment variables:
```bash
set AZURE_OPENAI_API_KEY=your-key
set AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
set AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
```

## CSV File Format Support

The application automatically detects:
- **Bank Account CSVs**: Files with "Posting Date" and "Balance" columns
- **Credit Card CSVs**: Files with "Transaction Date" and "Post Date" columns

All `.CSV` files in the data directory are processed automatically.

## Output

The application generates:
- **Excel Report**: `Transactions_Report_YYYYMMDD_HHMMSS.xlsx` in the data directory
- **Updated Mappings**: Saves your categorization patterns to `category_mappings.json`
- **Updated Categories**: Adds new subcategories to `categories.json` as needed

## Best Practices

1. **Start with the defaults**: The provided categories cover most common transactions
2. **Be consistent**: When categorizing similar transactions, use the same categories
3. **Use AI when available**: It learns from your previous categorizations
4. **Review suggestions**: Always review AI suggestions before accepting
5. **Organize subcategories**: Keep subcategories specific but not too granular
6. **Backup your mappings**: Regularly backup `category_mappings.json` - it contains all your learned patterns

## Customizing Categories

Edit `categories.json` to customize your category structure:

```json
{
  "categories": [
    {
      "name": "Your Custom Category",
      "subcategories": ["Sub1", "Sub2", "Sub3"],
      "inferenceRules": [
        {
          "subcategory": "Sub1",
          "keywords": ["keyword1", "keyword2"]
        }
      ]
    }
  ]
}
```

### Adding Inference Rules

Inference rules help automatically assign subcategories based on keywords in transaction descriptions. This is especially useful for transactions that come with a category but no subcategory (common in credit card CSVs).

**Example**: To automatically categorize parking transactions:
```json
{
  "name": "Transportation",
  "subcategories": ["Gas/Fuel", "Tolls", "Parking", "Other"],
  "inferenceRules": [
    {
      "subcategory": "Parking",
      "keywords": ["parking", "park", "garage"]
    }
  ]
}
```

When a transaction with category "Transportation" contains "parking" in its description, it will automatically be assigned the "Parking" subcategory.

**Tips for Inference Rules:**
- Keywords are case-insensitive
- Keywords are matched using "contains" logic
- More specific keywords should come before general ones
- If multiple rules match, the first matching rule wins
- Use partial words for better matching (e.g., "park" matches "parking", "parked", "park")

Changes take effect immediately on next run.

## Troubleshooting

### "Configuration directory not found"
- Make sure `categories.json` exists in the specified directory
- Or let the program create it in the default location

### "No CSV files found"
- Check that your CSV files have the `.CSV` extension (case-insensitive)
- Verify the data directory path is correct

### "Unknown CSV format"
- Ensure your CSV files match Chase bank account or credit card format
- Check that the first line contains proper headers
