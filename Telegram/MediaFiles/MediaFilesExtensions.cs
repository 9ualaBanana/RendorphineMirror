namespace Telegram.MediaFiles;

internal static class MediaFilesExtensions
{
    internal static IServiceCollection AddMediaFiles(this IServiceCollection services)
        => services
        .AddScoped<MediaFileDownloader>()
        .AddSingleton<CachedMediaFiles>();
}
