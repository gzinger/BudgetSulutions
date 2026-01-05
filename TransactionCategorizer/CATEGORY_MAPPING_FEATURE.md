# Category Mapping Feature

## Overview
The Category Mapping feature allows automatic conversion of categories and subcategories from CSV files (credit card statements) to your predefined category structure. This eliminates the need to manually remap common categories that credit card companies use differently than your budget structure.

## Problem Statement
Credit card companies categorize transactions using their own taxonomy, which often doesn't match your personal budget categories. For example:
- Credit card says: **"Gas"** (category only)
- Your budget needs: **"Transportation" / "Gas/Fuel"** (category/subcategory)

Without mapping, you'd need to manually recategorize every gas transaction or create individual patterns for each gas station.

## Solution
Configure category mappings in `categories.json` to automatically convert credit card categories to your budget categories.

## Configuration

### Location
**File:** `categories.json`

### Format
```json
{
  "categories": [ ... ],
  "categoryMappings": [
    {
      "originalCategory": "Gas",
      "originalSubcategory": "",
      "mappedCategory": "Transportation",
      "mappedSubcategory": "Gas/Fuel"
    }
  ]
}
```

### Mapping Rules

#### 1. Category-Only Mapping
Maps an entire original category to your category (subcategory determined by inference rules):

```json
{
  "originalCategory": "Gas",
  "originalSubcategory": "",
  "mappedCategory": "Transportation",
  "mappedSubcategory": "Gas/Fuel"
}
```

**Result:**
- Input: `Gas / (empty)`
- Output: `Transportation / Gas/Fuel`

#### 2. Category + Subcategory Mapping
Maps specific original category/subcategory combination:

```json
{
  "originalCategory": "Bills & Utilities",
  "originalSubcategory": "Phone",
  "mappedCategory": "Utilities",
  "mappedSubcategory": "Phone"
}
```

**Result:**
- Input: `Bills & Utilities / Phone`
- Output: `Utilities / Phone`

#### 3. Category Mapping with Empty Subcategory
Maps category but leaves subcategory to be determined by inference rules:

```json
{
  "originalCategory": "Food & Drink",
  "originalSubcategory": "",
  "mappedCategory": "Food & Dining",
  "mappedSubcategory": ""
}
```

**Result:**
- Input: `Food & Drink / (empty)`
- Output: `Food & Dining / (inferred from description)`

## Default Mappings

The system includes these default category mappings:

| Original Category | Original Subcategory | ? | Mapped Category | Mapped Subcategory |
|-------------------|---------------------|---|-----------------|-------------------|
| Gas | | ? | Transportation | Gas/Fuel |
| Travel | | ? | Transportation | Other |
| Fuel | | ? | Transportation | Gas/Fuel |
| Restaurants | | ? | Food & Dining | Restaurant |
| Dining | | ? | Food & Dining | Restaurant |
| Bills & Utilities | | ? | Utilities | |
| Bills & Utilities | Phone | ? | Utilities | Phone |
| Bills & Utilities | Internet | ? | Utilities | Internet/Cable |
| Professional Services | | ? | Other | Services |
| Personal | | ? | Other | Personal |
| Home | | ? | Housing | |
| Auto & Transport | | ? | Transportation | |
| Merchandise | | ? | Shopping | Retail |
| Food & Drink | | ? | Food & Dining | |

## Processing Logic

### Categorization Priority
```
1. User Mappings (category_mappings.json) - HIGHEST PRIORITY
2. Original Category from CSV
   2a. If original category exists ? Apply Category Mapping
   2b. Apply inference rules for subcategory
3. Default Patterns (built-in)
4. AI or User Input
```

### Matching Logic

When a transaction has an original category from CSV:

```csharp
1. Try exact match (Category + Subcategory)
   ? If found: Use mapped category and subcategory
   
2. Try category-only match
   ? If found: Use mapped category
   ? If mapped subcategory is empty: Use inference rules
   ? If mapped subcategory specified: Use it
   
3. No mapping found
   ? Keep original category
   ? Use inference rules for subcategory
```

### Example Flow

**Transaction from CSV:**
```
Description: "SHELL GAS STATION"
Category: "Gas"
Subcategory: ""
```

**Processing:**
1. Check user mappings ? No match
2. Check original category ? Found "Gas"
3. Apply category mapping:
   - Original: `Gas / `
   - Mapped: `Transportation / Gas/Fuel`
4. Result:
   - Category: `Transportation`
   - Subcategory: `Gas/Fuel`
   - Source: `CategoryMapping`
   - Pattern: `Gas`

## Adding Custom Mappings

### Step 1: Identify Original Categories
Look at your credit card CSV files to see what categories they use:
- Chase: "Gas", "Travel", "Restaurants"
- Amex: "Fuel", "Dining", "Merchandise"
- Discover: "Automotive", "Food & Drink"

### Step 2: Add Mapping Rules
Edit `categories.json` and add to `categoryMappings` array:

```json
{
  "categoryMappings": [
    // ...existing mappings...
    {
      "originalCategory": "Your Credit Card Category",
      "originalSubcategory": "",
      "mappedCategory": "Your Budget Category",
      "mappedSubcategory": "Your Budget Subcategory"
    }
  ]
}
```

### Step 3: Test
Run the categorizer and check the Excel output:
- **Categorization column** should show: `CategoryMapping: Original Category`
- **Category/Subcategory** should match your mapped values

## Excel Output

### Categorization Column Format

**With Category Mapping:**
```
CategoryMapping: Gas
CategoryMapping: Bills & Utilities/Phone
CategoryMapping: Food & Drink
```

**Examples:**
| Description | Original Cat | Original Subcat | Categorization | Category | Subcategory |
|------------|-------------|----------------|----------------|----------|-------------|
| Shell #1234 | Gas | | CategoryMapping: Gas | Transportation | Gas/Fuel |
| Optimum | Bills & Utilities | Phone | CategoryMapping: Bills & Utilities/Phone | Utilities | Phone |
| Restaurant | Dining | | CategoryMapping: Dining | Food & Dining | Restaurant |

## Use Cases

### Use Case 1: Different Credit Cards
**Problem:** You have multiple credit cards that use different category names

**Credit Card A:** Uses "Gas"
**Credit Card B:** Uses "Fuel"  
**Credit Card C:** Uses "Auto & Transport"

**Solution:** Create mappings for all variations:
```json
{
  "categoryMappings": [
    {
      "originalCategory": "Gas",
      "originalSubcategory": "",
      "mappedCategory": "Transportation",
      "mappedSubcategory": "Gas/Fuel"
    },
    {
      "originalCategory": "Fuel",
      "originalSubcategory": "",
      "mappedCategory": "Transportation",
      "mappedSubcategory": "Gas/Fuel"
    },
    {
      "originalCategory": "Auto & Transport",
      "originalSubcategory": "",
      "mappedCategory": "Transportation",
      "mappedSubcategory": ""
    }
  ]
}
```

### Use Case 2: Splitting Broad Categories
**Problem:** Credit card uses "Bills & Utilities" for everything, but you want granular subcategories

**Solution:** Use subcategory-specific mappings:
```json
{
  "categoryMappings": [
    {
      "originalCategory": "Bills & Utilities",
      "originalSubcategory": "Phone",
      "mappedCategory": "Utilities",
      "mappedSubcategory": "Phone"
    },
    {
      "originalCategory": "Bills & Utilities",
      "originalSubcategory": "Internet",
      "mappedCategory": "Utilities",
      "mappedSubcategory": "Internet/Cable"
    },
    {
      "originalCategory": "Bills & Utilities",
      "originalSubcategory": "",
      "mappedCategory": "Utilities",
      "mappedSubcategory": ""
    }
  ]
}
```

### Use Case 3: Consistent Naming
**Problem:** Credit card uses "Food & Drink" but you prefer "Food & Dining"

**Solution:**
```json
{
  "originalCategory": "Food & Drink",
  "originalSubcategory": "",
  "mappedCategory": "Food & Dining",
  "mappedSubcategory": ""
}
```

## Benefits

### 1. **Automatic Conversion**
No need to manually recategorize hundreds of transactions from credit card CSVs.

### 2. **Consistent Budgeting**
All transactions use your budget's category structure, regardless of source.

### 3. **Multi-Card Support**
Handle different category taxonomies from multiple credit cards seamlessly.

### 4. **Flexible Overrides**
User mappings in `category_mappings.json` still take precedence, allowing merchant-specific overrides.

### 5. **Transparent Tracking**
The Categorization column shows exactly which mapping was applied.

## Interaction with Other Features

### With User Mappings (category_mappings.json)
**Priority:** User mappings > Category mappings

If you have a specific pattern for "SHELL GAS" in category_mappings.json, it will be used instead of the category mapping for "Gas".

### With Inference Rules
Category mappings work together with inference rules:

```
1. Apply category mapping ? Get new category (and maybe subcategory)
2. If subcategory is empty ? Apply inference rules
```

Example:
```
Original: "Food & Drink" / ""
Mapped: "Food & Dining" / ""
Description: "STARBUCKS #1234"
Inference: keywords "starbucks" ? "Coffee"
Final: "Food & Dining" / "Coffee"
```

### With Pattern Tracking
Category mappings are tracked in the MatchedPattern field:
```
CategoryMapping: Gas
CategoryMapping: Bills & Utilities/Phone
```

## Troubleshooting

### Mapping Not Applied
**Symptom:** Transaction keeps original category

**Causes:**
1. User mapping exists for this transaction (takes precedence)
2. Category name doesn't match exactly (case-sensitive)
3. CategoryMappings not configured in categories.json

**Solution:**
1. Check category_mappings.json for conflicting patterns
2. Verify exact category name in CSV file
3. Ensure categoryMappings array exists in categories.json

### Wrong Subcategory
**Symptom:** Category maps correctly but subcategory is wrong

**Cause:** Mapped subcategory is empty, inference rules determine subcategory

**Solution:**
Specify the subcategory in the mapping:
```json
{
  "originalCategory": "Gas",
  "originalSubcategory": "",
  "mappedCategory": "Transportation",
  "mappedSubcategory": "Gas/Fuel"  ? Specify this
}
```

### Subcategory Not Preserved
**Symptom:** Original subcategory is lost

**Cause:** Category-only mapping without specific subcategory mapping

**Solution:** Add subcategory-specific mapping:
```json
{
  "originalCategory": "Bills & Utilities",
  "originalSubcategory": "Phone",
  "mappedCategory": "Utilities",
  "mappedSubcategory": "Phone"
}
```

## Technical Details

### Data Model
**File:** `TransactionCategorizer/Models/CategoryDefinition.cs`

```csharp
public class CategoryMappingRule
{
    public string OriginalCategory { get; set; }
    public string OriginalSubcategory { get; set; }
    public string MappedCategory { get; set; }
    public string MappedSubcategory { get; set; }
}

public class CategoriesConfiguration
{
    public List<CategoryDefinition> Categories { get; set; }
    public List<CategoryMappingRule>? CategoryMappings { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

### Matching Algorithm
```csharp
1. Load original category and subcategory from transaction
2. Search categoryMappings for exact match (category + subcategory)
3. If not found, search for category-only match
4. Apply mapped category and subcategory
5. Set CategorizationSource = "CategoryMapping"
6. Set MatchedPattern = original category(/subcategory)
```

### Performance
- **Memory:** Minimal (mappings loaded once)
- **Speed:** O(n) lookup where n = number of mappings (typically < 20)
- **Impact:** Negligible on overall processing time

## Future Enhancements

Potential improvements:
1. **Wildcard Matching:** Support regex in originalCategory
2. **Conditional Mappings:** Map based on amount or merchant
3. **Auto-Discovery:** Suggest mappings based on CSV analysis
4. **Import/Export:** Share mapping configurations
5. **Validation:** Warn about unmapped categories in CSV
6. **Statistics:** Report mapping effectiveness

## Summary

Category Mapping provides a powerful, configuration-driven way to normalize categories from various sources into your budget structure. Key features:

- ? **Automatic conversion** of credit card categories
- ? **Configurable** via categories.json
- ? **Flexible** matching (category-only or category+subcategory)
- ? **Transparent** tracking in Excel output
- ? **Compatible** with all other categorization features
- ? **Priority-aware** (respects user mappings)

No code changes needed - just update `categories.json` and rerun the categorizer!
