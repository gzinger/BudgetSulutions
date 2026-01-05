# Category Mapping Feature - Implementation Summary

## Overview
Implemented automatic mapping of original CSV categories/subcategories to predefined budget categories using configuration-based rules.

## Problem Solved
Credit card companies use different category names than your personal budget structure. For example:
- **Credit Card CSV:** `Gas` ? **Your Budget:** `Transportation / Gas/Fuel`
- **Credit Card CSV:** `Bills & Utilities / Phone` ? **Your Budget:** `Utilities / Phone`

Previously, you'd need to manually recategorize or create individual patterns for each transaction type.

## Solution Implemented

### 1. **New Data Model**
**File:** `TransactionCategorizer/Models/CategoryDefinition.cs`

Added `CategoryMappingRule` class and `CategoryMappings` property:
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
    public List<CategoryMappingRule>? CategoryMappings { get; set; }  // NEW
    public DateTime LastUpdated { get; set; }
}
```

### 2. **Configuration**
**File:** `categories.json`

Added `categoryMappings` array with 14 default mappings:
```json
{
  "categories": [ ... ],
  "categoryMappings": [
    {
      "originalCategory": "Gas",
      "originalSubcategory": "",
      "mappedCategory": "Transportation",
      "mappedSubcategory": "Gas/Fuel"
    },
    {
      "originalCategory": "Bills & Utilities",
      "originalSubcategory": "Phone",
      "mappedCategory": "Utilities",
      "mappedSubcategory": "Phone"
    }
    // ...12 more mappings
  ]
}
```

### 3. **Processing Logic**
**File:** `TransactionCategorizer/Services/CategorizationService.cs`

#### Updated Categorization Flow
```csharp
public async Task CategorizeTransactionsAsync(...)
{
    foreach (var transaction in transactions)
    {
        // 1. Try user mappings FIRST (highest priority)
        if (TryApplyUserMapping(transaction))
            continue;

        // 2. Original category from CSV
        if (!string.IsNullOrEmpty(transaction.Category) && 
            transaction.CategorizationSource == "Original")
        {
            // NEW: Apply category mapping
            if (TryApplyCategoryMapping(transaction))
            {
                transaction.CategorizationSource = "CategoryMapping";
            }
            
            // Continue with inference rules, etc.
        }
        
        // 3. Default patterns
        // 4. AI or user input
    }
}
```

#### New Method: TryApplyCategoryMapping
```csharp
private bool TryApplyCategoryMapping(Transaction transaction)
{
    // Try exact match (category + subcategory)
    var exactMatch = _categoriesConfig.CategoryMappings.FirstOrDefault(m =>
        m.OriginalCategory.Equals(transaction.Category, ...) &&
        m.OriginalSubcategory.Equals(transaction.Subcategory, ...));
    
    if (exactMatch != null) { ... }
    
    // Try category-only match
    var categoryMatch = _categoriesConfig.CategoryMappings.FirstOrDefault(m =>
        m.OriginalCategory.Equals(transaction.Category, ...) &&
        string.IsNullOrEmpty(m.OriginalSubcategory));
    
    if (categoryMatch != null) { ... }
    
    return false;
}
```

## Default Mappings Included

| Original Category | Original Subcat | ? | Mapped Category | Mapped Subcat |
|------------------|----------------|---|----------------|---------------|
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

## Features

### Two-Level Matching
1. **Exact Match:** Category + Subcategory
2. **Category Match:** Category only (subcategory via inference rules)

### Priority System
```
1. User Mappings (category_mappings.json) - HIGHEST
2. Original Category + Category Mapping
3. Default Patterns
4. AI or User Input
```

### Pattern Tracking
Category mappings are tracked in the `MatchedPattern` field:
```
CategoryMapping: Gas
CategoryMapping: Bills & Utilities/Phone
```

## Examples

### Example 1: Simple Category Mapping
**Input CSV:**
```
Description: SHELL GAS STATION #1234
Category: Gas
Subcategory: 
```

**Processing:**
1. Check user mappings ? No match
2. Check category mapping ? Found "Gas"
3. Apply mapping: `Gas` ? `Transportation / Gas/Fuel`

**Output:**
```
Category: Transportation
Subcategory: Gas/Fuel
Categorization: CategoryMapping: Gas
```

### Example 2: Category + Subcategory Mapping
**Input CSV:**
```
Description: OPTIMUM MOBILE
Category: Bills & Utilities
Subcategory: Phone
```

**Processing:**
1. Check user mappings ? No match
2. Check category mapping ? Found exact match
3. Apply mapping: `Bills & Utilities / Phone` ? `Utilities / Phone`

**Output:**
```
Category: Utilities
Subcategory: Phone
Categorization: CategoryMapping: Bills & Utilities/Phone
```

### Example 3: Category Mapping with Inference
**Input CSV:**
```
Description: STARBUCKS #5678
Category: Food & Drink
Subcategory: 
```

**Processing:**
1. Check user mappings ? No match
2. Check category mapping ? Found "Food & Drink"
3. Apply mapping: `Food & Drink` ? `Food & Dining / (empty)`
4. Apply inference rules: "starbucks" ? "Coffee"

**Output:**
```
Category: Food & Dining
Subcategory: Coffee
Categorization: CategoryMapping: Food & Drink
```

## Benefits

### ? Automatic Conversion
No manual recategorization of CSV categories

### ? Multi-Card Support
Handle different category taxonomies from multiple credit cards

### ? Consistent Budgeting
All transactions use your budget structure

### ? Configuration-Driven
Add/modify mappings without code changes

### ? Flexible
Works with exact or category-only matching

### ? Transparent
See which mapping was applied in Excel output

## Customization

### Adding New Mappings
Edit `categories.json`:
```json
{
  "categoryMappings": [
    {
      "originalCategory": "Your Credit Card Category",
      "originalSubcategory": "Optional Subcategory",
      "mappedCategory": "Your Budget Category",
      "mappedSubcategory": "Your Budget Subcategory"
    }
  ]
}
```

### Handling Multiple Credit Cards
```json
{
  "categoryMappings": [
    // Chase uses "Gas"
    {
      "originalCategory": "Gas",
      "originalSubcategory": "",
      "mappedCategory": "Transportation",
      "mappedSubcategory": "Gas/Fuel"
    },
    // Amex uses "Fuel"
    {
      "originalCategory": "Fuel",
      "originalSubcategory": "",
      "mappedCategory": "Transportation",
      "mappedSubcategory": "Gas/Fuel"
    },
    // Discover uses "Auto & Transport"
    {
      "originalCategory": "Auto & Transport",
      "originalSubcategory": "",
      "mappedCategory": "Transportation",
      "mappedSubcategory": ""
    }
  ]
}
```

## Excel Output

### Categorization Column
Shows the applied mapping:
```
CategoryMapping: Gas
CategoryMapping: Bills & Utilities/Phone
CategoryMapping: Food & Drink
```

### Full Example
| Description | Category | Subcategory | Categorization |
|------------|----------|-------------|----------------|
| Shell #1234 | Transportation | Gas/Fuel | CategoryMapping: Gas |
| Optimum | Utilities | Phone | CategoryMapping: Bills & Utilities/Phone |
| Starbucks | Food & Dining | Coffee | CategoryMapping: Food & Drink |

## Compatibility

### With Existing Features
? **User Mappings:** Take precedence over category mappings  
? **Inference Rules:** Work together for subcategory determination  
? **Pattern Tracking:** Category mappings tracked in MatchedPattern  
? **Default Patterns:** Applied if no category mapping exists  
? **AI Categorization:** Used as fallback

### Backward Compatibility
? Existing `categories.json` files work (CategoryMappings is optional)  
? No breaking changes to existing functionality  
? Gracefully handles missing CategoryMappings section

## Testing

### Test Scenarios
1. ? Category-only mapping (e.g., "Gas" ? "Transportation/Gas/Fuel")
2. ? Category + subcategory mapping (e.g., "Bills & Utilities/Phone" ? "Utilities/Phone")
3. ? Category mapping with empty subcategory (inference rules apply)
4. ? User mapping overrides category mapping
5. ? Missing category mapping (keeps original)
6. ? Case-insensitive matching

## Files Modified

1. **TransactionCategorizer/Models/CategoryDefinition.cs**
   - Added `CategoryMappingRule` class
   - Added `CategoryMappings` property to `CategoriesConfiguration`

2. **TransactionCategorizer/Services/CategorizationService.cs**
   - Updated `CategorizeTransactionsAsync` to call category mapping
   - Added `TryApplyCategoryMapping` method

3. **TransactionCategorizer/categories.json**
   - Added `categoryMappings` array with 14 default mappings

4. **TransactionCategorizer/CATEGORY_MAPPING_FEATURE.md**
   - Comprehensive documentation (new file)

## Performance Impact
- **Memory:** Minimal (mappings loaded once)
- **Speed:** O(n) lookup where n = mappings count (typically < 20)
- **Overall:** Negligible impact on processing time

## Future Enhancements
1. Wildcard/regex support in originalCategory
2. Conditional mappings (based on amount, merchant, etc.)
3. Auto-suggest mappings from CSV analysis
4. Validation warnings for unmapped categories
5. Import/export mapping configurations

## Summary

The Category Mapping feature provides a powerful, configuration-driven solution for normalizing categories from various sources. Key achievements:

- ? **Zero code needed** to add/modify mappings
- ? **14 default mappings** cover common credit card categories
- ? **Two-level matching** (exact or category-only)
- ? **Priority-aware** (respects user mappings)
- ? **Fully tracked** in Excel output
- ? **Backward compatible** with existing configurations

Users can now process CSV files from multiple credit cards and have all transactions automatically mapped to their consistent budget structure!
