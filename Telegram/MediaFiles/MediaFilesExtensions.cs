namespace Telegram.MediaFiles;

internal static class MediaFilesExtensions
{
    internal static IServiceCollection AddMediaFilesManager(this IServiceCollection services)
        => services
        .AddSingleton<MediaFileDownloader>()
        .AddSingleton<MediaFilesCache>()
        .AddSingleton<MediaFilesManager>();
}
