namespace Telegram.Telegram.Updates.Images.Models;

[Flags]
public enum ImageProcessingQueryFlags
{
    UploadImage = 1,
    UpscaleImage = 2,
    VectorizeImage = 4,
}
