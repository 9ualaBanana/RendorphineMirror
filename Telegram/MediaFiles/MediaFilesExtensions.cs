namespace Telegram.MediaFiles;

internal static class MediaFilesExtensions
{
    internal static IServiceCollection AddMediaFilesManager(this IServiceCollection services)
        => services
        .AddScoped<MediaFileDownloader>()
        .AddSingleton<MediaFilesCache>()
        .AddScoped<MediaFilesManager>();
}
