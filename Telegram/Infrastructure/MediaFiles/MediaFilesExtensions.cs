using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure.MediaFiles;

static class MediaFilesExtensions
{
    internal static ITelegramBotBuilder AddMediaFilesCore(this ITelegramBotBuilder builder)
    {
        builder.Services.TryAddScoped<MediaFile.Factory>();
        builder.Services.TryAddSingleton<MediaFilesCache>();
        builder.Services.TryAddSingleton<MediaFileDownloader>();

        return builder;
    }
}
