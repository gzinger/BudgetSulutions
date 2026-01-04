# Inference Rules Feature - Summary

## Overview
The `DetermineSubcategory` method has been refactored to remove hardcoded logic and use a flexible, configuration-based approach with inference rules defined in `categories.json`.

## Changes Made

### 1. **Updated `CategoryDefinition` Model**
Added support for inference rules:
```csharp
public class SubcategoryRule
{
    public string Subcategory { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
}

public class CategoryDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<string> Subcategories { get; set; } = new();
    public List<SubcategoryRule>? InferenceRules { get; set; }  // NEW
}
```

### 2. **Enhanced `categories.json`**
Added inference rules to each category:
```json
{
  "name": "Shopping",
  "subcategories": ["Online", "Retail", "Office Supplies", "Books", "Other"],
  "inferenceRules": [
    {
      "subcategory": "Online",
      "keywords": ["amazon", "amzn", "ebay", ".com", "online"]
    },
    {
      "subcategory": "Office Supplies",
      "keywords": ["staples", "office depot", "office"]
    }
  ]
}
```

### 3. **Refactored `DetermineSubcategory` Method**

**Before** (Hardcoded):
```csharp
private string DetermineSubcategory(Transaction transaction)
{
    var category = transaction.Category.ToLower();
    var desc = transaction.Description.ToLower();

    return category switch
    {
        "shopping" when desc.Contains("amazon") => "Online Shopping",
        "groceries" => "Supermarket",
        // ... 15+ hardcoded rules
        _ => "General"
    };
}
```

**After** (Configuration-based):
```csharp
private string DetermineSubcategory(Transaction transaction)
{
    var categoryDef = _categoriesConfig.Categories.FirstOrDefault(c => 
        c.Name.Equals(transaction.Category, StringComparison.OrdinalIgnoreCase));

    if (categoryDef == null)
        return "Other";

    if (categoryDef.InferenceRules != null && categoryDef.InferenceRules.Count > 0)
    {
        var descLower = transaction.Description.ToLower();
        
        foreach (var rule in categoryDef.InferenceRules)
        {
            if (rule.Keywords.Any(keyword => descLower.Contains(keyword.ToLower())))
            {
                return rule.Subcategory;
            }
        }
    }

    return categoryDef.Subcategories.FirstOrDefault() ?? "Other";
}
```

## How It Works

### 1. **Keyword Matching**
When a transaction has a category but no subcategory:
1. Find the category definition in `categories.json`
2. Check if the category has inference rules
3. Loop through each rule and check if any keyword matches the transaction description
4. Return the first matching subcategory
5. If no match, return the first subcategory in the list or "Other"

### 2. **Case-Insensitive Matching**
All keyword matching is case-insensitive for better accuracy.

### 3. **Flexible Configuration**
Users can now customize inference rules without touching code:
- Add new keywords for existing subcategories
- Create new inference rules for custom categories
- Adjust keyword priority by rule order

## Inference Rules by Category

### Income
- **Salary**: payroll, salary, wages
- **Interest**: interest
- **Refund**: refund, return, credit return

### Housing
- **Mortgage**: mortgage, mortg
- **Rent**: rent, rental
- **HOA**: hoa, association

### Utilities
- **Electric**: electric, electricity, power, con ed, consolidated edison
- **Gas**: national grid, keyspan, gas utility
- **Water**: water, sewer
- **Internet/Cable**: optimum, internet, cable, broadband, spectrum, comcast
- **Phone**: phone, ooma, verizon wireless, t-mobile, at&t

### Transportation
- **Gas/Fuel**: gas, fuel, sunoco, exxon, shell, bp, mobil, chevron
- **Tolls**: toll, ez-pass, ezpass
- **Parking**: parking, park
- **Public Transit**: subway, bus, metro, train, transit

### Groceries
- **Supermarket**: supermarket, market, grocery
- **Wholesale**: wholesale, costco, bjs, sam's club
- **Specialty**: specialty, organic, natural

### Food & Dining
- **Restaurant**: restaurant, dining, bistro, cafe, grill
- **Fast Food**: mcdonald, burger king, wendy, taco bell, kfc, subway
- **Takeout**: takeout, delivery, doordash, uber eats, grubhub
- **Coffee**: starbucks, dunkin, coffee, espresso

### Shopping
- **Online**: amazon, amzn, ebay, .com, online
- **Office Supplies**: staples, office depot, office
- **Books**: book, barnes, judaica

### Health & Wellness
- **Pharmacy**: pharmacy, cvs, walgreens, rite aid, prescription
- **Medical**: medical, doctor, hospital, clinic, health
- **Dental**: dental, dentist, orthodont
- **Fitness**: gym, fitness, yoga, trainer

### Insurance
- **Life**: life insurance, term life
- **Home**: home insurance, homeowner, property insurance
- **Auto**: auto insurance, car insurance, vehicle insurance
- **Health**: health insurance, medical insurance

### Gifts & Donations
- **Religious**: church, synagogue, temple, mosque, religious, congregation, chabad
- **Charity**: charity, donation, gofundme, fund, relief
- **Education**: yeshiva, school, education, scholarship
- **Gifts**: gift, present

### Financial
- **Transfer**: transfer, zelle, venmo, paypal, wire
- **Payment**: payment, autopay, bill pay
- **Fee**: fee, charge, service charge
- **ATM**: atm, withdrawal, cash

### Other
- **Entertainment**: entertainment, movie, theater, concert, tickets, netflix, spotify
- **Services**: service, laundry, cleaning, repair
- **Personal**: personal, haircut, salon, spa, barber

## Benefits

### 1. **No Code Changes Needed**
Users can customize subcategory inference by editing `categories.json` without recompiling.

### 2. **Easier Maintenance**
All categorization logic is in one place (configuration file) instead of scattered through code.

### 3. **Better Extensibility**
Adding new categories or subcategories automatically includes inference capability.

### 4. **User-Friendly**
Non-developers can adjust keyword matching to fit their specific transaction patterns.

### 5. **Consistent with Overall Architecture**
Aligns with the goal of moving configuration out of code and into JSON files.

## Usage Examples

### Example 1: Credit Card Transaction
```
Transaction: "STARBUCKS STORE #12345"
Category: "Food & Dining" (from CSV)
Subcategory: empty

Inference Process:
1. Find "Food & Dining" category
2. Check inference rules
3. Match keyword "starbucks" in description
4. Assign subcategory: "Coffee"
```

### Example 2: Bank Transaction
```
Transaction: "PAYROLL DEPOSIT - ACME CORP"
Category: "Income" (from pattern matching)
Subcategory: empty

Inference Process:
1. Find "Income" category
2. Check inference rules
3. Match keyword "payroll" in description
4. Assign subcategory: "Salary"
```

### Example 3: No Match
```
Transaction: "UNKNOWN MERCHANT LLC"
Category: "Shopping" (manually assigned)
Subcategory: empty

Inference Process:
1. Find "Shopping" category
2. Check inference rules
3. No keyword matches
4. Assign first subcategory: "Online"
```

## Migration Notes

### For Existing Users
- Inference rules are **optional** - categories work without them
- Existing category_mappings.json files continue to work
- The default categories.json includes comprehensive inference rules
- No action needed unless you want to customize the rules

### Backward Compatibility
- If `inferenceRules` is null or empty, the method falls back to the first subcategory
- Old categories.json files without inference rules still work
- JSON deserialization handles missing fields gracefully

## Future Enhancements

Potential improvements:
1. **Regex Support**: Allow regex patterns in keywords for more complex matching
2. **Priority Scoring**: Assign weights to rules for better conflict resolution
3. **Negative Keywords**: Support exclusion keywords (e.g., NOT "parking meter")
4. **Learning Mode**: Auto-suggest inference rules based on user categorizations
5. **Rule Statistics**: Track which rules are most frequently used
6. **Import/Export**: Share inference rule sets between users

## Testing Recommendations

1. **Test with existing transactions** that have categories but no subcategories
2. **Verify keyword matching** is case-insensitive
3. **Test fallback behavior** when no rules match
4. **Test with custom categories** without inference rules
5. **Validate JSON deserialization** with and without inference rules
