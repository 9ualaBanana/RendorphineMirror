namespace Telegram.Telegram.Updates.Images.Models;

public interface IContentType
{
    public static IContentType Image = new ImageContentType();
    public static IContentType Video = new VideoContentType();

    abstract string Extension { get; }
    abstract string MimeType { get; }
}
