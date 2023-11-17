namespace ChatGptApi.OpenAiApi;

public record ChatUsage(
    [property: JsonProperty("prompt_tokens")][param: JsonProperty("prompt_tokens")]
        int PromptTokens,
    [property: JsonProperty("completion_tokens")][param: JsonProperty("completion_tokens")]
        int CompletionTokens,
    [property: JsonProperty("total_tokens")][param: JsonProperty("total_tokens")]
        int TotalTokens
);
