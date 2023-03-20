using Telegram.Bot.Types;
using Telegram.Infrastructure.Middleware.UpdateRouting.MessageRouting;
using Telegram.MediaFiles;

namespace Telegram.Infrastructure.MediaFiles.Images;

static class ImagesExtensions
{
    internal static IServiceCollection AddImagesCore(this IServiceCollection services)
        => services
        .AddScoped<MessageRouter, ImagesRouterMiddleware>()
        .AddMediaFiles();

    internal static async Task<bool> IsImageAsync(this Message message, HttpClient httpClient, CancellationToken cancellationToken)
    {
        return message.Document.IsImage() || message.Photo is not null || await IsImageUrlAsync(message.Text);


        async Task<bool> IsImageUrlAsync(string? url) => Uri.IsWellFormedUriString(url, UriKind.Absolute) &&
            (await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            .Content.Headers.ContentType?.MediaType is string mediaType && mediaType.StartsWith("image");
    }

    internal static bool IsImage(this Document? document)
        => document is not null && document.MimeType is not null && document.MimeType.StartsWith("image");
}
