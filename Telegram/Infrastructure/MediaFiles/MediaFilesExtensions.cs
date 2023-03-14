namespace Telegram.Infrastructure.MediaFiles;

static class MediaFilesExtensions
{
    internal static IServiceCollection AddMediaFilesManager(this IServiceCollection services)
        => services
        .AddSingleton<MediaFilesManager>()
        .AddSingleton<MediaFileDownloader>()
        .AddSingleton<MediaFilesCache>();
}
