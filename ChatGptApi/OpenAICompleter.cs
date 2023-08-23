using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace ChatGptApi;

public class OpenAICompleter
{
    readonly OpenAIAPI Api;
    readonly ILogger Logger;

    decimal TotalSpent = 0;

    public OpenAICompleter(IConfiguration config, ILogger<OpenAICompleter> logger)
    {
        Logger = logger;

        var apikey = config.GetValue<string>("openai_apikey");
        Api = new OpenAIAPI(apikey);
    }

    async Task<string> SendChatRequest(string system, string message, double temperature = .1, int maxtokens = 400, int choices = 3, Model? model = null)
    {
        Logger.LogInformation($"Requesting \"{system}\" \"{message}\"");

        var req = new ChatRequest()
        {
            Model = model ?? Model.ChatGPTTurbo,
            Temperature = temperature,
            MaxTokens = maxtokens,
            NumChoicesPerMessage = choices,
            Messages = new[]
            {
                new ChatMessage(ChatMessageRole.System, system),
                new ChatMessage(ChatMessageRole.User, message),
            },
        };

        var completion = await Api.Chat.CreateChatCompletionAsync(req);

        Task.Run(() =>
        {
            if (completion.Usage is null) return;

            var encoder = SharpToken.GptEncoding.GetEncodingForModel("gpt-3.5-turbo");
            var prompttokens = encoder.Encode(system + message).Count;
            var outputtokens = encoder.Encode(string.Join("", completion.Choices.Select(c => c.Message.Content))).Count;

            var price = (prompttokens / 1000m * 0.0015m) + (outputtokens / 1000m * 0.002m);
            TotalSpent += price;
            Logger.LogInformation($"Tokens: Input {prompttokens}; Output {outputtokens}; Price ~${price}; Total this session: ~${TotalSpent}");
        }).Consume();

        var choice = completion.Choices
            .Select(c => c.Message.Content)
            .MaxBy(m => m.Length)
            .ThrowIfNull();

        Logger.LogInformation($"""
            From prompt "{system}"
            generated {completion.Choices.Count} choices:
            {string.Join('\n', completion.Choices.Select((c, i) => $"  {(c.Message.Content == choice ? "*" : " ")} {i}: {c.Message.Content}"))}
            """);

        choice = FilterString(choice);
        return choice;
    }

    const string PromptEnd = "to use in iStock. Use formal and dry language, do not use \"breathtaking\", \"majestic\" and alike. Do not include the keyword list in the result.";

    public async Task<string> GenerateNewTitle(IEnumerable<string> keywords)
    {
        var prompt = string.Join(", ", keywords);
        return await SendChatRequest($"Generate a title for an image using the provided keywords {PromptEnd}", prompt, maxtokens: 100);
    }

    public async Task<string> GenerateNewDescription(string title, IEnumerable<string> keywords)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        return await SendChatRequest($"Generate an extended title for an image using the provided title and keywords {PromptEnd}", prompt, maxtokens: 100);
    }

    public async Task<string> GenerateBetterTitle(string title, IEnumerable<string> keywords)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        return await SendChatRequest($"Generate another title for an image using the provided title and keywords {PromptEnd}", prompt, maxtokens: 100);
    }

    public async Task<string[]> GenerateBetterKeywords(string title, IEnumerable<string> keywords)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        return (await SendChatRequest($"Generate better keywords for an image using the provided title and keywords {PromptEnd}", prompt, maxtokens: 300))
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(kw => FilterString(kw).Replace("Keywords:", "").TrimStart())
            .ToArray();
    }

    static string FilterString(string str) =>
        str.Replace("\"", "").Replace("\'", "").Trim();
}
