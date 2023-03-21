using HeyRed.Mime;
using NLog;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using ILogger = NLog.ILogger;

namespace Telegram.Infrastructure.MediaFiles;

/// <summary>
/// Represents a media file stored in one of the supported by Telegram forms.
/// </summary>
/// <remarks>
/// Media file can be the one stored on Telegram servers or at any other URL.
/// </remarks>
public sealed class MediaFile
{
    internal readonly long? Size;

    internal Extension Extension { get; }

    internal string MimeType { get; }

    /// <summary>
    /// Identifier for the file stored on Telegram servers which can be used to download or reuse this file.
    /// </summary>
    internal readonly string? FileId;

    /// <summary>
    /// URL where the file is stored and can be downloaded from.
    /// </summary>
    internal readonly Uri? Location;

    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    #region Initialization

    MediaFile(long? size, Extension extension, string fileId)
    {
        Size = size;
        Extension = extension;
        MimeType = MimeTypesMap.GetMimeType(extension.ToString());
        FileId = fileId;
        Location = null;
    }

    MediaFile(Extension extension, Uri location)
    {
        Size = null;
        Extension = extension;
        MimeType = MimeTypesMap.GetMimeType(extension.ToString());
        FileId = null;
        Location = location;
    }

    /// <inheritdoc cref="FromDocumentAttachedTo(Message)"/>
    internal static MediaFile From(Message message)
    {
        if (message.Video is Video video)  // Check for video must precede the one for image because Photo is not null for videos too.
            return From(video);
        else if (message.Photo is PhotoSize[] images)
            return From(images.Last()); // Original image with the highest resolution.
#pragma warning disable IDE0150 // Prefer 'null' check over type check
        if (message.Document is Document)
            return FromDocumentAttachedTo(message);
#pragma warning restore IDE0150 // Prefer 'null' check over type check
        else if (Uri.IsWellFormedUriString(message.Text, UriKind.Absolute))
            return From(new Uri(message.Text));
        else
        {
            string errorMessage = $"Received {nameof(Message)} doesn't represent a media file.";
            var exception = new ArgumentException(errorMessage);
            _logger.Fatal(exception);
            throw exception;
        }
    }

    static MediaFile From(PhotoSize image) => new(image.FileSize, Extension.jpeg, image.FileId);
    static MediaFile From(Video video) => new(video.FileSize, Extension.mp4, video.FileId);
    /// <exception cref="ArgumentException">Extension of the document can't be deduced.</exception>
    static MediaFile FromDocumentAttachedTo(Message message)
    {
        var document = message.Document!;

        Extension extension;
        if (document.FileName is string fileName)
            if (Enum.TryParse(Path.GetExtension(fileName).TrimStart('.'), ignoreCase: true, out extension) ||
                Enum.TryParse(Path.GetExtension(message.Caption?.Trim(' ', '.')), ignoreCase: true, out extension)
                )
                return new(document.FileSize, extension, document.FileId);

        throw new ArgumentException("Extension of the document can't be deduced.", nameof(extension));
    }
    // Resource URL can point only to an image.
    internal static MediaFile From(Uri imageUrl) => new(Extension.jpeg, imageUrl);

    #endregion

    #region Conversions

    public static implicit operator InputOnlineFile(MediaFile this_) => this_.FileId is not null ? this_.FileId : this_.Location!.ToString();

    #endregion
}
