using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace Telegram.Services.Telegram.Updates.Images;

public record TelegramImage(int? Size, string? FileId = null, string? Url = null)
{
    public InputOnlineFile InputOnlineFile => Url is not null ? Url : FileId!;

    public static TelegramImage From(Message message) => message!.Document is not null ?
        From(message.Document!) : FromCompressedPhotoOrUrl(message);
    static TelegramImage FromCompressedPhotoOrUrl(Message message) => Uri.IsWellFormedUriString(message.Text, UriKind.Absolute) ?
        new(null, Url: message.Text) : From(message.Photo!.Last());
    static TelegramImage From(PhotoSize photo) => new(photo.FileSize, FileId: photo.FileId);
    static TelegramImage From(Document document) => new(document.FileSize, document.FileId);
    public static TelegramImage From(InputOnlineFile inputOnlineFile) => inputOnlineFile.Url is not null ?
        new(null, Url: inputOnlineFile.Url) : new (null, FileId: inputOnlineFile.FileId);


    public async Task Download(string path, TelegramBot bot)
    {
        using var downloadedImage = System.IO.File.Create(path);

        if (FileId is not null)
            await bot.GetInfoAndDownloadFileAsync(FileId, downloadedImage);
        else
            await (await bot.HttpClient.GetStreamAsync(Url)).CopyToAsync(downloadedImage);
    }
}
