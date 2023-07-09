namespace Node.Tasks.Models.ExecInfo;

public class GenerateImageByPromptInfo
{
    [Default(ImageGenerationSource.StableDiffusion)]
    public ImageGenerationSource Source { get; }

    public string Prompt { get; init; }

    [JsonProperty("negprompt")]
    public string? NegativePrompt { get; init; }

    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? Seed { get; init; }

    public GenerateImageByPromptInfo(ImageGenerationSource source, string prompt)
    {
        Source = source;
        Prompt = prompt;
    }
}
