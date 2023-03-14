namespace Telegram.Infrastructure.MediaFiles;

public class MediaFilesManager
{
    internal readonly MediaFileDownloader Downloader;
    internal readonly MediaFilesCache Cache;

    public MediaFilesManager(MediaFileDownloader downloader, MediaFilesCache cache)
    {
        Downloader = downloader;
        Cache = cache;
    }
}
