# Transaction Categorizer

A C# application that reads bank and credit card transaction CSV files, categorizes them, and generates an Excel report grouped by Credit/Debit, Category, and Subcategory.

## Features

- Reads Chase bank account CSV files (format: `Details,Posting Date,Description,Amount,Type,Balance,Check or Slip #`)
- Reads Chase credit card CSV files (format: `Transaction Date,Post Date,Description,Category,Type,Amount,Memo`)
- Automatic categorization using:
  - Pattern matching for common transactions (50+ predefined patterns)
  - Optional AI categorization (OpenAI or Azure OpenAI)
  - User input for unrecognized transactions
- **Smart Learning**: Saves user categorizations immediately and uses them as context for AI
- Generates detailed Excel report with:
  - Summary sheet with totals by category
  - Separate sheets for Credits and Debits
  - Transactions grouped by Category ? Subcategory
  - Sorted by Provider then Date within each group
  - Subtotals for each group

## Prerequisites

- .NET 8.0 SDK or later
- (Optional) OpenAI API key or Azure OpenAI credentials for AI-assisted categorization

## Building

```bash
cd TransactionCategorizer
dotnet build
```

## Running

### Basic Usage (Pattern matching + User prompts)
```bash
dotnet run
```

This will use built-in pattern matching and prompt you for any unrecognized transactions.

### With OpenAI AI Categorization

#### Option 1: Using Windows Command Prompt
```cmd
set OPENAI_API_KEY=sk-your-api-key-here
dotnet run
```

#### Option 2: Using PowerShell
```powershell
$env:OPENAI_API_KEY = "sk-your-api-key-here"
dotnet run
```

#### Option 3: Set Permanently (Windows)
1. Open System Properties (Windows Key + Pause/Break)
2. Click "Advanced system settings"
3. Click "Environment Variables"
4. Under "User variables", click "New"
5. Variable name: `OPENAI_API_KEY`
6. Variable value: `sk-your-api-key-here`
7. Click OK
8. Restart your terminal/command prompt
9. Run `dotnet run`

### With Azure OpenAI

#### Setup Azure OpenAI Environment Variables

**Using Command Prompt:**
```cmd
set AZURE_OPENAI_API_KEY=your-azure-api-key
set AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
set AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
dotnet run
```

**Using PowerShell:**
```powershell
$env:AZURE_OPENAI_API_KEY = "your-azure-api-key"
$env:AZURE_OPENAI_ENDPOINT = "https://your-resource.openai.azure.com/"
$env:AZURE_OPENAI_DEPLOYMENT = "gpt-4o-mini"
dotnet run
```

**To Set Permanently (Windows):**
1. Follow the same steps as OpenAI above, but add these three variables:
   - `AZURE_OPENAI_API_KEY` - Your Azure OpenAI API key
   - `AZURE_OPENAI_ENDPOINT` - Your Azure OpenAI endpoint URL
   - `AZURE_OPENAI_DEPLOYMENT` - Your deployment name (e.g., gpt-4o-mini, gpt-4, gpt-35-turbo)

#### How to Get Azure OpenAI Credentials

1. **API Key & Endpoint:**
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to your Azure OpenAI resource
   - Go to "Keys and Endpoint" section
   - Copy "KEY 1" (or KEY 2) for `AZURE_OPENAI_API_KEY`
   - Copy "Endpoint" for `AZURE_OPENAI_ENDPOINT`

2. **Deployment Name:**
   - In your Azure OpenAI resource, go to "Model deployments"
   - Click "Manage Deployments" (opens Azure AI Studio)
   - Note the "Deployment name" of your model (e.g., gpt-4o-mini)
   - Use this for `AZURE_OPENAI_DEPLOYMENT`

#### How to Get OpenAI API Key

1. Go to [OpenAI Platform](https://platform.openai.com)
2. Sign in or create an account
3. Navigate to [API Keys](https://platform.openai.com/api-keys)
4. Click "Create new secret key"
5. Copy the key (starts with `sk-`)
6. **Important:** Save it securely - you won't be able to see it again!

### Specify Custom Directory
```bash
dotnet run "C:\path\to\csv\files"
```

## Input Files

Place your CSV files in the same directory as the application (or parent directory):

**Bank Account Files:**
- `Chase1047_Activity_20250304.CSV`
- `Chase4645_Activity_20250304.CSV`
- `Chase6759_Activity_20250304.CSV`

**Credit Card Files:**
- `Chase2783_Activity20240101_20241231_20250305.CSV`
- `Chase3383_Activity20240101_20241231_20250305.CSV`

## Output

- `Transactions_Report_YYYYMMDD_HHMMSS.xlsx` - Excel report
- `category_mappings.json` - Saved category mappings for future use

## User Prompts

When an unrecognized transaction is found, you'll be prompted:

```
============================================================
Transaction: SOME VENDOR DESCRIPTION
Amount: ($123.45)
Date: 1/15/2024
Account: Bank - 1047
============================================================

Common categories:
  1. Utilities    2. Transportation    3. Groceries    4. Shopping
  5. Food & Drink    6. Gifts & Donations    7. Income    8. Transfers
  ...

Enter category (number/custom), 's' to skip, 'a' to skip all:
```

### Options:
- **Enter a number (1-17)**: Select a common category
- **Type a custom category name**: Create your own category
- **Type `s`**: Skip this transaction (marks as "Uncategorized")
- **Type `a`**: Skip all remaining uncategorized transactions

### AI-Assisted Prompts

When AI is enabled and finds a suggestion:

```
AI categorized 'SOME VENDOR DESCRIPTION...'
  -> Category/Subcategory (Provider: Vendor Name)
Accept? (Y/n/e to edit):
```

- **Press Enter or `Y`**: Accept AI suggestion (automatically saved for future use)
- **Type `n`**: Reject and you'll be prompted to enter manually
- **Type `e`**: Edit the AI suggestion

**Note:** All user inputs are automatically saved to `category_mappings.json` and will be used:
1. For pattern matching in future runs
2. As examples for the AI to maintain consistency

## Categories

Common categories used:
- **Utilities** - Electrical, Gas, Internet/Cable, Phone
- **Transportation** - Tolls, Gas, Parking
- **Groceries** - Supermarket
- **Shopping** - Online Shopping, Books, Office Supplies
- **Food & Drink** - Restaurant
- **Gifts & Donations** - Religious, Charity, Non-Profit
- **Income** - Salary, Interest
- **Transfers** - Internal Transfer, Zelle
- **Housing** - Mortgage, Rent/HOA
- **Insurance** - Life Insurance, Home Insurance
- **Health & Wellness** - Pharmacy, Medical
- **Entertainment** - Streaming, Sports/Recreation
- **Services** - Professional Services, Laundry
- **Bills & Utilities** - Bill Payment
- **Credit Card** - Payment
- **Loans** - Education Loan
- **Cash** - ATM Withdrawal

## Customization

Edit `category_mappings.json` to add or modify category patterns:

```json
{
  "Mappings": [
    {
      "Pattern": "VENDOR NAME",
      "Category": "Category Name",
      "Subcategory": "Subcategory Name",
      "Provider": "Provider Display Name"
    }
  ]
}
```

Patterns support regular expressions for flexible matching.

## Troubleshooting

### AI Not Working
- Verify your API key is set correctly: `echo %OPENAI_API_KEY%` (CMD) or `$env:OPENAI_API_KEY` (PowerShell)
- Check for typos in environment variable names
- For Azure OpenAI, ensure all three variables are set (KEY, ENDPOINT, DEPLOYMENT)
- Restart your terminal after setting environment variables

### Files Not Found
- Make sure CSV files are in the parent directory of the application
- Or specify the directory: `dotnet run "C:\path\to\csv\files"`

### Excel Generation Fails
- Ensure you have write permissions in the output directory
- Check that the output file isn't already open in Excel

## Tips for Best Results

1. **First Run**: The application will prompt for many transactions. Take time to categorize them correctly.
2. **Subsequent Runs**: Your saved mappings will automatically categorize similar transactions.
3. **With AI**: The AI learns from your previous categorizations and maintains consistency.
4. **Review Mappings**: Periodically review `category_mappings.json` to refine patterns.
5. **Pattern Matching**: The application automatically creates patterns from your inputs to match similar transactions.
