namespace Node.Tasks.Models.ExecInfo;

[JsonConverter(typeof(StringEnumConverter))]
public enum ImageGenerationSource
{
    StableDiffusion,
}
