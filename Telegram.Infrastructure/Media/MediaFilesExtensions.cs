using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.MediaFiles;

namespace Telegram.Infrastructure.Media;

static class MediaFilesExtensions
{
    internal static ITelegramBotBuilder AddMediaFiles(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped<MediaFile.Factory>();
        builder.Services.TryAddSingleton<MediaFilesCache>();
        builder.Services.TryAddSingleton<MediaFile.Downloader>();

        return builder;
    }
}
