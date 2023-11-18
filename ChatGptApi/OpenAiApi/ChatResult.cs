namespace ChatGptApi.OpenAiApi;

public record ChatResult(ChatUsage Usage, IReadOnlyList<ChatChoice> Choices);
