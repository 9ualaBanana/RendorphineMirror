namespace Telegram.Telegram.Updates.Images.Models;

public interface IContentType
{
    public static readonly IContentType Image = new ImageContentType();
    public static readonly IContentType Video = new VideoContentType();

    abstract string Extension { get; }
    abstract string MimeType { get; }
}
