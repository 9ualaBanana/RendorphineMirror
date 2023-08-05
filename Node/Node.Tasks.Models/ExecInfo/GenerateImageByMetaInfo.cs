namespace Node.Tasks.Models.ExecInfo;

public class GenerateImageByMetaInfo
{
    [Default(ImageGenerationSource.StableDiffusion)]
    public ImageGenerationSource Source { get; }

    public GenerateImageByMetaInfo(ImageGenerationSource source) => Source = source;
}
