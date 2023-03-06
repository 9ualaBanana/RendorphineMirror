using NLog;
using System.Net.Mime;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using ILogger = NLog.ILogger;

namespace Telegram.Models;

/// <summary>
/// Represents a media file stored in one of the supported by Telegram forms.
/// </summary>
/// <remarks>
/// Media file can be the one stored on Telegram servers or at any other URL.
/// </remarks>
public sealed class MediaFile
{
    internal readonly long? Size;

    internal string Extension { get; }

    internal string? MimeType { get; }

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

    MediaFile(long? size, string extension, string? mimeType, string fileId)
    {
        Size = size;
        Extension = extension;
        MimeType = mimeType;
        FileId = fileId;
        Location = null;
    }

    MediaFile(string extension, string? mimeType, Uri location)
    {
        Size = null;
        Extension = extension;
        MimeType = mimeType;
        FileId = null;
        Location = location;
    }

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
            string errorMessage = $"BUG: {nameof(Message)} doesn't represent a media file.";
            var exception = new ArgumentException(errorMessage);
            _logger.Error(exception);
            throw exception;
        }
    }

    static MediaFile From(PhotoSize image) => new(image.FileSize, Extensions.Jpeg, MediaTypeNames.Image.Jpeg, image.FileId);
    static MediaFile From(Video video) => new(video.FileSize, Extensions.Mp4, "video/mp4", video.FileId);
    static MediaFile FromDocumentAttachedTo(Message message)
    {
        var document = message.Document!;

        string? extension = null;
        if (document.FileName is string fileName)
            extension = Path.GetExtension(fileName);
        extension ??= Path.GetExtension(message.Caption?.Trim());

        if (extension is null)
            throw new ArgumentNullException(nameof(extension), "Extension of the document can't be deduced.");

        return new(document.FileSize, extension, document.MimeType, document.FileId);
    }
    // Resource URL can point only to an image.
    internal static MediaFile From(Uri imageUrl) => new(Extensions.Jpeg, MediaTypeNames.Image.Jpeg, imageUrl);

    #endregion

    #region Conversions

    public static implicit operator InputOnlineFile(MediaFile this_) => this_.FileId is not null ? this_.FileId : this_.Location!.ToString();

    #endregion
}
