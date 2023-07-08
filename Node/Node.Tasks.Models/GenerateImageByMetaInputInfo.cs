namespace Node.Tasks.Models;

public class GenerateImageByMetaInputInfo
{
    public string Prompt { get; init; }
    public string? NegativePrompt { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }

    public int? Seed { get; init; }

    public GenerateImageByMetaInputInfo(string prompt) => Prompt = prompt;
}
