using Telegram.Bot.Types.InputFiles;
using Telegram.MPlus;

namespace Telegram.Tasks.ResultPreview;

/// <summary>
/// Preview of a task result media file uploaded to M+.
/// </summary>
/// <remarks>
/// Implicitly convertible to <see cref="InputOnlineFile"/>.
/// </remarks>
internal abstract record TaskResultFromMPlus
{
    /// <inheritdoc cref="MPlusFileInfo"/>
    internal readonly MPlusFileInfo FileInfo;

    /// <summary>
    /// ID of the task that was responsible for producing the task result that this <see cref="TaskResultFromMPlus"/> represents.
    /// </summary>
    internal readonly string TaskId;

    /// <summary>
    /// Name of the node that was responsible for producing the task result that this <see cref="TaskResultFromMPlus"/> represents.
    /// </summary>
    internal readonly string TaskExecutor;

    internal readonly Uri FileDownloadLink;

    /// <summary>
    /// Static factory for constructing <see cref="TaskResultFromMPlus"/> instances.
    /// </summary>
    internal static TaskResultFromMPlus Create(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
        => mPlusFileInfo.Type switch
        {
            MPlusFileType.Raster or MPlusFileType.Vector => new ImageTaskResultFromMPlus(mPlusFileInfo, taskExecutor, downloadLink),
            MPlusFileType.Video => new VideoTaskResultFromMPlus(mPlusFileInfo, taskExecutor, downloadLink),
            _ => throw new NotImplementedException()
        };

    protected TaskResultFromMPlus(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
    {
        FileInfo = mPlusFileInfo;
        TaskId = (string)FileInfo["extid"]!;
        TaskExecutor = taskExecutor;
        FileDownloadLink = downloadLink;
    }

    public static implicit operator InputOnlineFile(TaskResultFromMPlus this_) => new(this_.FileDownloadLink);
}
