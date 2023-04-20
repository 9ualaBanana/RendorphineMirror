using HeyRed.Mime;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Infrastructure.MediaFiles.Images;

namespace Telegram.Infrastructure.MediaFiles;

/// <summary>
/// Represents a media file stored in one of the supported by Telegram forms.
/// </summary>
/// <remarks>
/// Media file can be stored on Telegram servers and identified with <see cref="FileId"/> or at any other <see cref="Location"/>.
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

    #endregion

    #region Conversions

    public static implicit operator InputOnlineFile(MediaFile this_) => this_.FileId is not null ? this_.FileId : this_.Location!.ToString();

    #endregion


    public class Factory
    {
        readonly HttpClient _httpClient;

        readonly ILogger _logger;

        public Factory(IHttpClientFactory httpClientFactory, ILogger<Factory> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        internal async Task<MediaFile> CreateAsyncFrom(Message message, CancellationToken cancellationToken)
        {
            if (message.Video is Video video)  // Check for video must precede the one for image because Photo is not null for videos too.
                return CreateFrom(video);
            else if (message.Photo is PhotoSize[] image)
                return CreateFrom(image);
#pragma warning disable IDE0150 // Prefer 'null' check over type check
            if (message.Document is Document)
                return CreateFromDocumentAttachedTo(message);
#pragma warning restore IDE0150 // Prefer 'null' check over type check
            else if (await ImagesHelper.IsImageUriAsync(message.Text, _httpClient, cancellationToken))
                return await CreateAsyncFrom(new Uri(message.Text!), cancellationToken);
            else
            {
                string errorMessage = $"{nameof(message)} doesn't represent {nameof(MediaFile)}.";
                var exception = new ArgumentException(errorMessage);
                _logger.LogCritical(exception, string.Empty);
                throw exception;
            }
        }

        /// <summary>
        /// Creates <see cref="MediaFile"/> from an array of images sent via Telegram
        /// representing a single image with different resolutions sorted from lowest to highest.
        /// </summary>
        /// <param name="image">
        /// Array of images representing a single image with different resolutions sorted from lowest to highest
        /// from the last of which <see cref="MediaFile"/> will be created.
        /// </param>
        /// <remarks>Resulting <see cref="MediaFile"/> will have <see cref="Extension.jpeg"/> as all <see cref="PhotoSize"/> shall have it.</remarks>
        /// <returns><see cref="MediaFile"/> created from the highest resolution <paramref name="image"/>.</returns>
        static MediaFile CreateFrom(PhotoSize[] image) => CreateFrom(image.Last());

        /// <summary>
        /// Creates <see cref="MediaFile"/> from an image sent via Telegram.
        /// </summary>
        /// <param name="image">Image from which <see cref="MediaFile"/> will be created.</param>
        /// <returns><see cref="MediaFile"/> created from the <paramref name="image"/>.</returns>
        static MediaFile CreateFrom(PhotoSize image) => new(image.FileSize, Extension.jpeg, image.FileId);

        static MediaFile CreateFrom(Video video) => new(video.FileSize, Extension.mp4, video.FileId);

        /// <exception cref="ArgumentException"><see cref="Common.Extension"/> of the document can't be deduced.</exception>
        static MediaFile CreateFromDocumentAttachedTo(Message message)
        {
            var document = message.Document!;

            if (document.MimeType is string mimeType && Enum.TryParse(MimeTypesMap.GetExtension(mimeType), ignoreCase: true, out Extension extension) ||
                document.FileName is string fileName && Enum.TryParse(Path.GetExtension(fileName).TrimStart('.'), ignoreCase: true, out extension) ||
                message.Caption is string caption && Enum.TryParse(caption.Trim('.', ' '), ignoreCase: true, out extension))
                    return new(document.FileSize, extension, document.FileId);
            
            throw new ArgumentException("Extension of the document attached to the message can't be deduced.", nameof(message));
        }

        internal async Task<MediaFile> CreateAsyncFrom(Uri imageUri, CancellationToken cancellationToken)
        {
            try { return await CreateAsyncCoreFrom(imageUri); }
            catch (Exception exception)
            { _logger.LogCritical(exception, $"Couldn't create {nameof(MediaFile)} from {nameof(imageUri)}"); throw; }


            async Task<MediaFile> CreateAsyncCoreFrom(Uri imageUri)
            {
                var resourceMimeType = await _httpClient.GetMimeTypeAsync(imageUri, cancellationToken);

                if (ImagesHelper.IsImageMimeType(resourceMimeType))

                    if (Enum.TryParse(resourceMimeType, ignoreCase: true, out Extension extension))
                        return new MediaFile(extension, imageUri);
                    else throw new ArgumentOutOfRangeException(nameof(imageUri),
                        $"{nameof(imageUri)} refers to a resource with unknown extension based on its MIME type: {resourceMimeType}."
                        );

                else throw new ArgumentException(
                    $"{nameof(imageUri)} doesn't refer to an image based on MIME type of the resource.", nameof(imageUri)
                    );
            }
        }
    }
}
