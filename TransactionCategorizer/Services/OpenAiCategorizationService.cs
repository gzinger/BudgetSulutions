using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using TransactionCategorizer.Models;

namespace TransactionCategorizer.Services;

/// <summary>
/// AI categorization using Azure OpenAI or OpenAI
/// </summary>
public class OpenAiCategorizationService : IAiCategorizationService
{
    private readonly ChatClient _chatClient;
    private readonly string _mappingsFilePath;

    private string GetSystemPrompt()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("You are a financial transaction categorizer. Given a bank or credit card transaction description, determine:");
        sb.AppendLine("1. Category (e.g., Utilities, Transportation, Groceries, Shopping, Food & Drink, Gifts & Donations, Income, Transfers, Housing, Insurance, Health & Wellness, Entertainment, Services, Bills & Utilities, Credit Card, Loans, Cash, Other)");
        sb.AppendLine("2. Subcategory (more specific, e.g., Electrical, Gas, Tolls, Supermarket, Online Shopping, etc.)");
        sb.AppendLine("3. Provider/Merchant name (clean, readable name)");
        sb.AppendLine("4. Confidence (0.0 to 1.0)");
        sb.AppendLine();
        sb.AppendLine("Respond ONLY with valid JSON in this exact format:");
        sb.AppendLine("{\"category\": \"string\", \"subcategory\": \"string\", \"provider\": \"string\", \"confidence\": 0.0}");

        // Load existing mappings to provide context
        var existingMappings = LoadExistingMappings();
        if (existingMappings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Here are some examples of how similar transactions have been categorized by the user:");
            
            foreach (var m in existingMappings.Take(20))
            {
                sb.AppendLine($"- Pattern '{m.Pattern}' -> Category: '{m.Category}', Subcategory: '{m.Subcategory}', Provider: '{m.Provider}'");
            }
            
            sb.AppendLine();
            sb.AppendLine("Use these examples to guide your categorization, maintaining consistency with the user's preferences.");
        }

        return sb.ToString();
    }

    private List<CategoryMapping> LoadExistingMappings()
    {
        if (!string.IsNullOrEmpty(_mappingsFilePath) && File.Exists(_mappingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_mappingsFilePath);
                var mappings = JsonSerializer.Deserialize<CategoryMappings>(json);
                return mappings?.Mappings ?? new List<CategoryMapping>();
            }
            catch
            {
                return new List<CategoryMapping>();
            }
        }
        return new List<CategoryMapping>();
    }

    public OpenAiCategorizationService(string apiKey, string? endpoint = null, string model = "gpt-4o-mini", string? mappingsFilePath = null)
    {
        _mappingsFilePath = mappingsFilePath ?? string.Empty;

        if (!string.IsNullOrEmpty(endpoint))
        {
            // Azure OpenAI
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
            _chatClient = azureClient.GetChatClient(model);
        }
        else
        {
            // OpenAI
            var openAiClient = new OpenAI.OpenAIClient(apiKey);
            _chatClient = openAiClient.GetChatClient(model);
        }
    }

    public async Task<AiCategorizationResult?> CategorizeAsync(string description)
    {
        try
        {
            var systemPrompt = GetSystemPrompt();

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Categorize this transaction: {description}")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.3f,
                MaxOutputTokenCount = 200
            };

            var response = await _chatClient.CompleteChatAsync(messages, options);
            var content = response.Value.Content[0].Text;

            // Parse JSON response
            var result = JsonSerializer.Deserialize<AiCategorizationResult>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI Error: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Fallback service that uses pattern matching without AI
/// </summary>
public class FallbackCategorizationService : IAiCategorizationService
{
    public Task<AiCategorizationResult?> CategorizeAsync(string description)
    {
        // Return null to indicate AI couldn't help - will fall back to user input
        return Task.FromResult<AiCategorizationResult?>(null);
    }
}
