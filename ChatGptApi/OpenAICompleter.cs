using System.Text.RegularExpressions;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace ChatGptApi;

public partial class OpenAICompleter
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

    async Task<ChatResult> SendChatRequestResult(string system, string message, double temperature = .1, int maxtokens = 400, int choices = 3, Model? model = null)
    {
        if (model is null || model.ModelID is null)
            model = Model.ChatGPTTurbo;

        Logger.LogInformation($"Requesting from {model.ModelID} \"{system}\" \"{message}\"");

        var req = new ChatRequest()
        {
            Model = model,
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

            var encoder = SharpToken.GptEncoding.GetEncodingForModel(model.ModelID);
            var prompttokens = encoder.Encode(system + message).Count;
            var outputtokens = encoder.Encode(string.Join("", completion.Choices.Select(c => c.Message.Content))).Count;

            var price = prompttokens / 1000m * 0.0015m + outputtokens / 1000m * 0.002m;
            TotalSpent += price;
            Logger.LogInformation($"Tokens: Input {prompttokens}; Output {outputtokens}; Price ~${price}; Total this session: ~${TotalSpent}");
        }).Consume();

        Logger.LogInformation($"""
            From prompt "{system}"
            generated {completion.Choices.Count} choices:
            {string.Join('\n', completion.Choices.Select((c, i) => $"  {i}: {c.Message.Content}"))}
            """);

        return completion;
    }
    async Task<string> SendChatRequest(string system, string message, double temperature = .1, int maxtokens = 400, int choices = 3, Model? model = null)
    {
        var completion = await SendChatRequestResult(system, message, temperature, maxtokens, choices, model);
        return FilterString(completion.Choices
            .Select(c => c.Message.Content)
            .MaxBy(m => m.Length)
            .ThrowIfNull());
    }

    const string PromptEndBase = "to use in iStock. Use formal and dry language, do not use \"breathtaking\", \"majestic\", \"captivating\" and alike.";
    const string PromptEnd = $"{PromptEndBase} Do not include the keyword list in the result.";
    static readonly char[] KeywordSeparators = new[] { ',', '\n' };

    public async Task<string> GenerateNewTitle(IEnumerable<string> keywords, string? system = null, string? model = null)
    {
        var prompt = string.Join(", ", keywords);
        return await SendChatRequest(system ?? $"Generate a title for an image using the provided keywords {PromptEnd}", prompt, maxtokens: 100, model: model);
    }

    public async Task<string> GenerateNewDescription(string title, IEnumerable<string> keywords, string? system = null, string? model = null)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        return await SendChatRequest(system ?? $"Generate an extended title for an image using the provided title and keywords {PromptEnd}", prompt, maxtokens: 100, model: model);
    }

    public async Task<string> GenerateBetterTitle(string title, IEnumerable<string> keywords, string? system = null, string? model = null)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        return await SendChatRequest(system ?? $"Generate another title for an image using the provided title and keywords {PromptEnd}", prompt, maxtokens: 100, model: model);
    }

    public async Task<string[]> GenerateBetterKeywords(string title, IEnumerable<string> keywords, string? system = null, string? model = null)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        var response = await SendChatRequestResult(system ?? $"Generate a set of 50 one-word keywords for an image based on the provided title and keywords {PromptEndBase}", prompt, maxtokens: 300, model: model);
        var kws = response.Choices
            .SelectMany(choice => FilterString(choice.Message.Content)
                .Split(KeywordSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(FilterKeyword)
                .WhereNotNull()
            ).Distinct()
            .ToArray();

        return kws;
    }

    static string FilterString(string str) => str.Replace("\"", "").Replace("\'", "").Replace("Keywords:", "").Trim();
    static string? FilterKeyword(string str)
    {
        // remove all garbage before keyword ('1. kw', '- kw')
        foreach (var match in NonWordStuffAtStartRegex().Matches(str).AsEnumerable())
            str = str.Substring(match.Index + match.Length);

        // if still not a single word
        if (!SingleWordRegex().IsMatch(str))
            return null;

        return str.ToLowerInvariant();
    }

    [GeneratedRegex(@"^[\W\d]*")]
    private static partial Regex NonWordStuffAtStartRegex();

    [GeneratedRegex(@"^[A-Za-z]*$")]
    private static partial Regex SingleWordRegex();
}
