using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace Telegram.Telegram.Updates.Images.Models;

public record TelegramMediaFile(int? Size, string Extension, string MimeType, string? FileId = null, string? Url = null) : IContentType
{
    public InputOnlineFile InputOnlineFile => Url is not null ? Url : FileId!;


    public static TelegramMediaFile From(Message message) => message!.Document is not null ?
        From(message.Document!) : FromAttachmentOrUrl(message);
    static TelegramMediaFile FromAttachmentOrUrl(Message message) => Uri.IsWellFormedUriString(message.Text, UriKind.Absolute) ?
        From(message.Text) : message.Video is null ?
        From(message.Photo!.Last()) : From(message.Video);
    static TelegramMediaFile From(PhotoSize image) => new(image.FileSize, IContentType.Image.Extension, IContentType.Image.MimeType, image.FileId);
    static TelegramMediaFile From(Video video) => new(video.FileSize, IContentType.Video.Extension, IContentType.Video.MimeType, video.FileId);
    static TelegramMediaFile From(Document imageDocument) => new(imageDocument.FileSize, IContentType.Image.Extension, IContentType.Image.MimeType, imageDocument.FileId);
    static TelegramMediaFile From(string imageUri) => new(null, IContentType.Image.Extension, IContentType.Image.MimeType, Url: imageUri);


    public async Task Download(string path, HttpClient httpClient, TelegramBot bot)
    {
        using var downloadedImage = System.IO.File.Create(path);

        if (FileId is not null)
            await bot.GetInfoAndDownloadFileAsync(FileId, downloadedImage);
        else
            await (await httpClient.GetStreamAsync(Url)).CopyToAsync(downloadedImage);
    }
}
