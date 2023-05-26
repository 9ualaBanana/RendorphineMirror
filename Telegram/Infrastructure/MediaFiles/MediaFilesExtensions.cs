using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure.MediaFiles;

static class MediaFilesExtensions
{
    internal static ITelegramBotBuilder AddMediaFilesCore(this ITelegramBotBuilder builder)
    {
        builder.Services
            .AddScoped<MediaFile.Factory>()
            .AddSingleton<MediaFilesCache>()
            .AddSingleton<MediaFileDownloader>();
        return builder;
    }
}
