namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

internal record VideoTaskResultPreview : TaskResultPreview
{
    internal int Width;
    internal int Height;

    internal long Mp4Size;
    internal string Mp4Url;

    internal long WebmSize;
    internal string WebmUrl;


    internal VideoTaskResultPreview(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
        : base(mPlusFileInfo, taskExecutor, downloadLink)
    {
        var videoInfo = mPlusFileInfo["videopreview"]!;
        Width = (int)videoInfo["width"]!;
        Height = (int)videoInfo["height"]!;

        Mp4Size = (long)videoInfo["mp4"]!["size"]!;
        Mp4Url = (string)videoInfo["mp4"]!["url"]!;

        WebmSize = (long)videoInfo["webm"]!["size"]!;
        WebmUrl = (string)videoInfo["webm"]!["url"]!;
    }
}
