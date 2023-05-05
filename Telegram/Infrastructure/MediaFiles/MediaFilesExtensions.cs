namespace Telegram.Infrastructure.MediaFiles;

static class MediaFilesExtensions
{
    internal static IServiceCollection AddMediaFiles(this IServiceCollection services)
        => services
        .AddScoped<MediaFile.Factory>()
        .AddSingleton<MediaFileDownloader>()
        .AddSingleton<MediaFilesCache>();
}
