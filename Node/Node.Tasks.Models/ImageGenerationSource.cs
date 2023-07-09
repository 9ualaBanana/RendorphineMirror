namespace Node.Tasks.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum ImageGenerationSource
{
    StableDiffusion,
}
