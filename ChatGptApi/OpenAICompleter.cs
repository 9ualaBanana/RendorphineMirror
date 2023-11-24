using System.Text.RegularExpressions;
using ChatGptApi.OpenAiApi;

namespace ChatGptApi;

public partial class OpenAICompleter
{
    readonly ChatApi Api;
    readonly GoogleCloudApi GoogleApi;
    readonly ILogger Logger;

    decimal TotalSpent = 0;

    public OpenAICompleter(IConfiguration config, ILogger<OpenAICompleter> logger)
    {
        Logger = logger;
        Api = new ChatApi(config.GetValue<string>("openai_apikey").ThrowIfNull());
        GoogleApi = new GoogleCloudApi("gcredentials-vertex.json");
    }

    async Task<ChatResult> SendChatRequestResult(IReadOnlyList<ChatRequest.IMessage> messages, double temperature = .1, int maxtokens = 400, int choices = 3, string? model = null)
    {
        model ??= ChatModels.Gpt35Turbo;
        Logger.LogInformation($"Requesting from model: {model}, messages: [ {string.Join(", ", messages.Select(m => $"'{m.Role}: {m.AsString()}'"))} ]");

        var req = new ChatRequest()
        {
            Model = model,
            Temperature = temperature,
            MaxTokens = maxtokens,
            NumChoicesPerMessage = choices,
            Messages = messages,
        };

        var completion = await Api.SendRequest(req);

        if (completion.Usage is not null)
        {
            var prompttokens = completion.Usage.PromptTokens;
            var outputtokens = completion.Usage.CompletionTokens;

            var price = prompttokens / 1000m * 0.0015m + outputtokens / 1000m * 0.002m;
            TotalSpent += price;
            Logger.LogInformation($"Tokens: Input {prompttokens}; Output {outputtokens}; Price ~${price}; Total this session: ~${TotalSpent}");
        }

        var img = messages.OfType<ChatRequest.ImageMessage>().FirstOrDefault()?.Content.OfType<ChatRequest.ImageMessageContent>().FirstOrDefault();
        Logger.LogInformation($"""
            From prompt '{messages.OfType<ChatRequest.ImageMessage>().FirstOrDefault()?.Content.OfType<ChatRequest.TextMessageContent>().FirstOrDefault()?.Text}' {(img is null ? string.Empty : $"with an image (size {img.ImageUrl.Url.Length}, {img.ImageUrl.Detail} detail)")}
            generated {completion.Choices.Count} choices:
            {string.Join('\n', completion.Choices.Select((c, i) => $"  {i}: {c.Message?.Content}"))}
            """);

        return completion;
    }
    async Task<ChatResult> SendChatRequestResult(string system, string message, double temperature = .1, int maxtokens = 400, int choices = 3, string? model = null)
    {
        var messages = new[]
        {
            new ChatRequest.TextMessage(ChatRole.System, system),
            new ChatRequest.TextMessage(ChatRole.User, message),
        };

        return await SendChatRequestResult(messages, temperature, maxtokens, choices, model);
    }
    async Task<string> SendChatRequest(IReadOnlyList<ChatRequest.IMessage> messages, double temperature = .1, int maxtokens = 400, int choices = 3, string? model = null)
    {
        var completion = await SendChatRequestResult(messages, temperature, maxtokens, choices, model);
        return FilterString(completion.Choices
            .Select(c => c.Message.Content)
            .MaxBy(m => m.Length)
            .ThrowIfNull());
    }
    async Task<string> SendChatRequest(string system, string message, double temperature = .1, int maxtokens = 400, int choices = 3, string? model = null)
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


    public async Task<TK> GenerateTKChatGpt(ChatRequest.ImageMessageContent image, string? prompt)
    {
        prompt ??= $$"""
            Generate a set of 50 one-word keywords and a title for the provided image {{PromptEndBase}}
            Write result as a JSON in the following format:
            ```json
            { "title": "string", "keywords": [ "string", "string" ] }
            ```
            """;
        var messages = new[]
        {
            new ChatRequest.ImageMessage(ChatRole.User, new ChatRequest.IMessageContent[]
            {
                new ChatRequest.TextMessageContent(prompt),
                image,
            }),
        };

        var title = null as string;
        var keywords = Array.Empty<string>();

        for (int retry = 0; retry < 3; retry++)
        {
            var response = await SendChatRequestResult(messages, choices: 1, maxtokens: 300, model: ChatModels.Gpt4Vision);
            var jsonstr = response.Choices[0].Message.Content;

            if (jsonstr.StartsWith("```json"))
                jsonstr = jsonstr.AsSpan()["```json".Length..].Trim().ToString();
            jsonstr = jsonstr.Replace("`", string.Empty).Trim();

            var tk = JsonConvert.DeserializeObject<TK>(jsonstr);
            if (tk is not { Keywords.Count: > 0, Title: not null })
            {
                Logger.LogInformation($"Received invalid tk: '{jsonstr}', retrying ({retry + 1}/3)");
                continue;
            }

            title ??= tk.Title;
            keywords = keywords.Concat(tk.Keywords).Distinct().ToArray();

            //if (keywords.Length >= 50)
            return new TK(title, keywords);

            //Logger.LogInformation($"Not enough keywords ({keywords.Length}), retrying");
        }

        throw new Exception("Could not generate the tk");
    }

    public async Task<TK> GenerateNewTKVision(byte[] image, IEnumerable<string> labels, string? system, string? model)
    {
        var imgbase64 = Convert.ToBase64String(image);
        var req = new GoogleCloudApi.GoogleRequest(new(1, "en"), new[] { new GoogleCloudApi.GoogleRequest.GoogleRequestInstances(new(imgbase64)) });
        var gresponse = await GoogleApi.SendRequest(req);
        var prediction = gresponse.Predictions[0];

        system ??= $$"""
            Generate a set of 50 one-word keywords and a title for the provided title and keywords {{PromptEndBase}}.
            Title: {caption}.
            Keywords: {labels}.

            Write result as a JSON in the following format:
            ```json
            { "title": "string", "keywords": [ "string", "string" ] }
            ```
            """;

        system = system
            .Replace("{labels}", string.Join(", ", labels))
            .Replace("{caption}", prediction);

        var title = null as string;
        var keywords = Array.Empty<string>();

        for (int retry = 0; retry < 3; retry++)
        {
            var prompt = "";
            var response = await SendChatRequestResult(system, prompt, choices: 1, maxtokens: 300, model: model);
            var jsonstr = response.Choices[0].Message.Content;

            if (jsonstr.StartsWith("```json"))
                jsonstr = jsonstr.AsSpan()["```json".Length..].Trim().ToString();
            jsonstr = jsonstr.Replace("`", string.Empty).Trim();

            var tk = JsonConvert.DeserializeObject<TK>(jsonstr);
            if (tk is not { Keywords.Count: > 0, Title: not null })
            {
                Logger.LogInformation($"Received invalid tk: '{jsonstr}', retrying ({retry + 1}/3)");
                continue;
            }

            title ??= tk.Title;
            keywords = keywords.Concat(tk.Keywords).Distinct().ToArray();

            //if (keywords.Length >= 50)
            return new TK(title, keywords);

            //Logger.LogInformation($"Not enough keywords ({keywords.Length}), retrying");
        }

        throw new Exception("Could not generate the tk");
    }

    public async Task<string> GenerateNewDescription(string title, IEnumerable<string> keywords, string? system, string? model)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        return await SendChatRequest(system ?? $"Generate an extended title for an image using the provided title and keywords {PromptEnd}", prompt, maxtokens: 100, model: model);
    }

    public async Task<string> GenerateBetterTitle(string title, IEnumerable<string> keywords, string? system, string? model)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        return await SendChatRequest(system ?? $"Generate another title for an image using the provided title and keywords {PromptEnd}", prompt, maxtokens: 100, model: model);
    }

    public async Task<string[]> GenerateBetterKeywords(string title, IEnumerable<string> keywords, string? system, string? model)
    {
        var prompt = $"""
            Title: {title}
            Keywords: {string.Join(", ", keywords)}
        """;

        var response = await SendChatRequestResult(system ?? $"Generate a set of 50 one-word keywords for an image based on the provided title and keywords {PromptEndBase}", prompt, maxtokens: 300, model: model);
        return ProcessKeywords(response).ToArray();
    }

    static string[] ProcessKeywords(ChatResult result)
    {
        var kws = result.Choices.ThrowIfNull()
            .SelectMany(choice => FilterString((choice.Message?.Content).ThrowIfNull())
                .Split(KeywordSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(FilterKeyword)
                .WhereNotNull()
            ).Distinct()
            .ToArray();

        if (kws.Length == 0)
            throw new Exception($"Could not generate keywords; The model responded with: {result.Choices.FirstOrDefault()?.Message.Content ?? "<nothing>"}");

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
