namespace Telegram.Telegram.Updates.Images.Models;

public class VideoContentType : IContentType
{
    public string Extension => ".mp4";

    public string MimeType => "video/mp4";
}
