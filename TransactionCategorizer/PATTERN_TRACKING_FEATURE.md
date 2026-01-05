# Pattern Tracking Feature

## Overview
The Categorization column in Excel reports now shows not only the categorization source (UserMapping, Pattern, AI, User, etc.) but also the **exact pattern** that matched the transaction.

## Implementation

### New Fields in Transaction Model

**File:** `TransactionCategorizer/Models/Transaction.cs`

```csharp
public class Transaction
{
    // ...existing fields...
    
    // Pattern tracking
    public string CategorizationSource { get; set; } = string.Empty;  // "Original", "Pattern", "AI", "User"
    public string MatchedPattern { get; set; } = string.Empty;         // The exact pattern that matched
    
    // Helper property for display
    public string CategorizationDetails => string.IsNullOrEmpty(MatchedPattern) 
        ? CategorizationSource 
        : $"{CategorizationSource}: {MatchedPattern}";
}
```

### Pattern Capture Logic

Patterns are captured when transactions are categorized:

#### 1. User Mappings (from category_mappings.json)
```csharp
transaction.CategorizationSource = "UserMapping";
transaction.MatchedPattern = mapping.Pattern;  // e.g., "AMAZON|AMZN|Amazon"
```

#### 2. Default Patterns (built-in)
```csharp
transaction.CategorizationSource = "Pattern";
transaction.MatchedPattern = pattern;  // e.g., "CONSOLIDATED EDISON|CON ?ED"
```

#### 3. AI or User Categorization
```csharp
transaction.CategorizationSource = "User" or "AI";
transaction.MatchedPattern = CreatePatternFromDescription(description);
// e.g., "STARBUCKS.*STORE" (auto-generated pattern)
```

#### 4. Original from CSV
```csharp
transaction.CategorizationSource = "Original";
transaction.MatchedPattern = "";  // No pattern, came from source file
```

## Excel Output Examples

### Categorization Column Display

| Transaction Description | Categorization Column Value |
|------------------------|---------------------------|
| AMAZON MKTP US*123456 | UserMapping: AMAZON\|AMZN\|Amazon |
| CON EDISON 12/30 | Pattern: CONSOLIDATED EDISON\|CON ?ED |
| STARBUCKS STORE #1234 | User: STARBUCKS.*STORE |
| Netflix Subscription | AI: Netflix.*Subscription |
| Shopping (from credit card CSV) | Original |
| Uncategorized transaction | Skipped |

### Visual Example in Excel

```
Date      | Provider  | Description          | Amount  | Categorization
----------|-----------|---------------------|---------|----------------------------------
01/15/25  | Amazon    | AMAZON MKTP US*456  | -$45.99 | UserMapping: AMAZON|AMZN|Amazon
01/16/25  | Con Ed    | CON EDISON 12/30    | -$123.45| Pattern: CONSOLIDATED EDISON|CON ?ED
01/17/25  | Starbucks | STARBUCKS #5678     | -$6.50  | User: STARBUCKS.*STORE
01/18/25  | Target    | TARGET T-1234       | -$89.99 | Original
```

## Benefits

### 1. **Transparency**
You can see exactly which rule triggered the categorization:
- **UserMapping patterns**: Shows your custom regex from category_mappings.json
- **Default patterns**: Shows built-in patterns
- **Auto-generated patterns**: Shows the pattern created from user input

### 2. **Debugging**
Easy to identify why a transaction was categorized a certain way:
```
UserMapping: AMAZON|AMZN|Amazon
```
Tells you that the transaction matched your user mapping for Amazon.

### 3. **Pattern Refinement**
When you see multiple transactions that should match but don't:
```
Transaction 1: UserMapping: STARBUCKS.*STORE
Transaction 2: Uncategorized (STARBUCK COFFEE SHOP)
```
You can update your pattern to: `STARBUCKS?.*` to catch both.

### 4. **Audit Trail**
Full visibility into categorization decisions:
- **Original**: Came from bank/credit card CSV
- **UserMapping**: Your custom patterns
- **Pattern**: Built-in patterns
- **AI**: AI suggested and saved
- **User**: Manually categorized and saved

## Use Cases

### Use Case 1: Verify Pattern Matching
**Problem**: Transaction not categorized as expected

**Solution**: Check the Categorization column
```
Expected: UserMapping for "Whole Foods"
Actual: Pattern: AMAZON|AMZN|Amazon

Diagnosis: Transaction description was "AMAZON FRESH AT WHOLE FOODS"
Fix: Create more specific pattern for Whole Foods
```

### Use Case 2: Optimize Patterns
**Problem**: Multiple similar patterns

**Before:**
```
UserMapping: STARBUCKS
UserMapping: STARBUCKS.*STORE
UserMapping: STARBUCKS.*COFFEE
```

**After:** Consolidate to:
```
UserMapping: STARBUCKS.*
```

### Use Case 3: Track Auto-Generated Patterns
When you manually categorize a transaction, the system creates a pattern:
```
User: NETFLIX.*Subscription
```
You can see what pattern was auto-generated and refine it in category_mappings.json if needed.

## Column Width in Excel

The Categorization column is automatically set to appropriate widths:
- **Detailed sheets (Credits/Debits)**: 50 characters wide
- **All Transactions sheet**: 50 characters wide

This accommodates most pattern descriptions while keeping the spreadsheet readable.

## Pattern Format Examples

### Simple Patterns
```
AMAZON
TARGET
COSTCO
```

### Regex Patterns
```
AMAZON|AMZN|Amazon
CONSOLIDATED EDISON|CON ?ED
STARBUCKS.*STORE
GOOGLE.*YouTube|YouTube
```

### Auto-Generated Patterns
When you categorize manually, patterns are created from the description:
```
Original: "NETFLIX STREAMING SERVICE 12/30"
Generated Pattern: "NETFLIX.*STREAMING.*SERVICE"
```

### Complex Patterns
```
DEPT EDUCATION|STUDENT LN
WITHDRAWAL \d+/\d+
CHECK \d+
TEMU\.COM.*WWW\.TEMU\.COM
```

## Troubleshooting

### Pattern Not Showing
**Symptom**: Categorization column shows only source (e.g., "UserMapping") without pattern

**Cause**: Transaction was categorized before this feature was added

**Solution**: Re-run the categorizer with the updated version

### Pattern Too Long
**Symptom**: Pattern text is cut off in Excel

**Solution**: 
1. Double-click the column border to auto-fit
2. Or manually widen the Categorization column
3. Pattern is fully preserved in the cell, just not fully visible

### Wrong Pattern Matched
**Symptom**: Transaction matched unexpected pattern

**Example:**
```
Description: "BP GAS STATION"
Expected Pattern: "BP.*BP" (gas station)
Actual Pattern: "BRICK OVEN|PIZZA|RESTAURANT"
```

**Solution**: 
1. Check pattern order in category_mappings.json
2. More specific patterns should come before general ones
3. Adjust pattern to be more precise

## Technical Details

### Pattern Storage
- **MatchedPattern**: Stored in Transaction object
- **Persisted**: In Excel output only (not in database)
- **Format**: Raw regex pattern string

### Pattern Display
- **Property**: `CategorizationDetails`
- **Format**: `"{Source}: {Pattern}"` or just `"{Source}"` if no pattern
- **Examples**:
  - `"UserMapping: AMAZON|AMZN|Amazon"`
  - `"Pattern: CONSOLIDATED EDISON|CON ?ED"`
  - `"Original"` (no pattern)
  - `"Skipped"` (no pattern)

### Performance Impact
- **Memory**: Minimal (one string per transaction)
- **Storage**: Negligible increase in Excel file size
- **Speed**: No measurable impact on processing time

## Future Enhancements

Potential improvements:
1. **Pattern Effectiveness Statistics**: Track which patterns match most frequently
2. **Pattern Conflict Detection**: Identify overlapping patterns
3. **Pattern Suggestions**: AI-powered pattern optimization
4. **Pattern Library**: Share effective patterns between users
5. **Pattern Testing**: Test patterns against transaction history
6. **Visual Pattern Editor**: GUI for creating and testing regex patterns

## Summary

The pattern tracking feature provides full transparency into how transactions are categorized, making it easier to:
- **Debug categorization issues**
- **Optimize pattern matching**
- **Understand categorization decisions**
- **Refine custom patterns**
- **Audit categorization sources**

All pattern information is automatically captured and displayed in the Excel Categorization column without any user action required.
