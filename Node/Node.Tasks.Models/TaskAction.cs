namespace Node.Tasks.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskAction
{
    EsrganUpscale,
    EditVideo,
    EditRaster,
    GenerateQSPreview,
    VeeeVectorize,
    GreenscreenBackground,
    GenerateTitleKeywords,
}
