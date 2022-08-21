namespace ReepoBot.Services.Telegram.Updates.Images;

[Flags]
public enum ImageProcessingQueryFlags
{
    Upload = 1, // Makes ToString() work properly (join enum flag values with ',').
    Upscale
}
