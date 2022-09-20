namespace Telegram.Telegram.Updates.Images.Models;

internal class ImageContentType : IContentType
{
    public string Extension => ".jpg";

    public string MimeType => "image/jpeg";
}
