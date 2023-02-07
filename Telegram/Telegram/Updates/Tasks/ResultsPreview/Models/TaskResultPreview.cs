using Telegram.Bot.Types.InputFiles;

namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

internal abstract record TaskResultPreview
{
    internal readonly MPlusFileInfo FileInfo;
    internal readonly string TaskId;
    /// <summary>
    /// Name of the node that was responsible for producing the task result that this <see cref="TaskResultPreview"/> describes.
    /// </summary>
    internal readonly string TaskExecutor;
    internal readonly Uri FileDownloadLink;

    internal static TaskResultPreview Create(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
        => mPlusFileInfo.Type switch
        {
            MPlusFileType.Raster or MPlusFileType.Vector => new ImageTaskResultPreview(mPlusFileInfo, taskExecutor, downloadLink),
            MPlusFileType.Video => new VideoTaskResultPreview(mPlusFileInfo, taskExecutor, downloadLink),
            _ => throw new NotImplementedException()
        };

    protected TaskResultPreview(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
    {
        FileInfo = mPlusFileInfo;
        TaskId = (string)FileInfo["id"]!;
        TaskExecutor = taskExecutor;
        FileDownloadLink = downloadLink;
    }

    public static implicit operator InputOnlineFile(TaskResultPreview this_) => new(this_.FileDownloadLink);
}
