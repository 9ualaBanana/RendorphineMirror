using NLog;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using ILogger = NLog.ILogger;

namespace Telegram.Telegram.Updates.Images.Models;

internal sealed class TelegramMediaFile : IContentType
{
    internal readonly long? Size;

    public string Extension { get; }

    public string MimeType { get; }

    /// <summary>
    /// Identifier for the file stored on Telegram servers which can be used to download or reuse this file.
    /// </summary>
    internal readonly string? FileId;

    /// <summary>
    /// URL where the file is stored and can be downloaded from.
    /// </summary>
    internal readonly Uri? Source;

    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    TelegramMediaFile(long? size, string extension, string mimeType, string fileId)
        : this(size, extension, mimeType, fileId, null)
    {
    }

    TelegramMediaFile(string extension, string mimeType, Uri source)
        : this(null, extension, mimeType, null, source)
    {
    }
    
    TelegramMediaFile(long? size, string extension, string mimeType, string? fileId, Uri? source)
    {
        Debug.Assert(FileId is not null || Source is not null);

        Size = size;
        Extension = extension;
        MimeType = mimeType;
        FileId = fileId;
        Source = source;
    }

    internal static TelegramMediaFile From(Message message)
    {
        if (message.Document is Document document)
            return From(document);
        else if (message.Video is Video video)
            return From(video);
        else if (message.Photo is PhotoSize[] images)
            return From(images.Last()/*Original image with the highest resolution*/);
        else if (Uri.IsWellFormedUriString(message.Text, UriKind.Absolute))
            return From(new Uri(message.Text));
        else
        {
            string errorMessage = $"{nameof(Message)} doesn't represent a media file.";
            var exception = new ArgumentException(errorMessage);
            _logger.Error(exception);
            throw exception;
        }
    }

    static TelegramMediaFile From(PhotoSize image) => new(image.FileSize, IContentType.Image.Extension, IContentType.Image.MimeType, image.FileId);
    static TelegramMediaFile From(Video video) => new(video.FileSize, IContentType.Video.Extension, IContentType.Video.MimeType, video.FileId);
    static TelegramMediaFile From(Document imageDocument) => new(imageDocument.FileSize, IContentType.Image.Extension, IContentType.Image.MimeType, imageDocument.FileId);
    internal static TelegramMediaFile From(Uri imageUrl) => new(IContentType.Image.Extension, IContentType.Image.MimeType, imageUrl);


    internal async Task DownloadAsyncTo(string destinationPath, HttpClient httpClient, TelegramBot bot, CancellationToken cancellationToken)
    {
        using var downloadedMediaFile = System.IO.File.Create(destinationPath);

        if (FileId is not null)
            await bot.GetInfoAndDownloadFileAsync(FileId, downloadedMediaFile, cancellationToken);
        else await
                (await httpClient.GetStreamAsync(Source, cancellationToken))
                .CopyToAsync(downloadedMediaFile, cancellationToken);
    }

    public static implicit operator InputOnlineFile(TelegramMediaFile this_) => this_.FileId is not null ? this_.FileId : this_.Source!.ToString();
}
