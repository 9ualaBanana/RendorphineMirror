using Telegram.Bot.Types.InputFiles;

namespace Telegram.Tasks;

/// <summary>
/// Preview of a task result media file uploaded to M+.
/// </summary>
/// <remarks>
/// Implicitly convertible to <see cref="InputOnlineFile"/>.
/// </remarks>
internal abstract record TaskResultPreviewFromMPlus
{
    /// <inheritdoc cref="MPlusFileInfo"/>
    internal readonly MPlusFileInfo FileInfo;

    /// <summary>
    /// ID of the task that was responsible for producing the task result that this <see cref="TaskResultPreviewFromMPlus"/> represents.
    /// </summary>
    internal readonly string TaskId;

    /// <summary>
    /// Name of the node that was responsible for producing the task result that this <see cref="TaskResultPreviewFromMPlus"/> represents.
    /// </summary>
    internal readonly string TaskExecutor;

    internal readonly Uri FileDownloadLink;

    /// <summary>
    /// Static factory for constructing <see cref="TaskResultPreviewFromMPlus"/> instances.
    /// </summary>
    internal static TaskResultPreviewFromMPlus Create(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
        => mPlusFileInfo.Type switch
        {
            MPlusFileType.Raster or MPlusFileType.Vector => new ImageTaskResultPreviewFromMPlus(mPlusFileInfo, taskExecutor, downloadLink),
            MPlusFileType.Video => new VideoTaskResultPreviewFromMPlus(mPlusFileInfo, taskExecutor, downloadLink),
            _ => throw new NotImplementedException()
        };

    protected TaskResultPreviewFromMPlus(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
    {
        FileInfo = mPlusFileInfo;
        TaskId = (string)FileInfo["id"]!;
        TaskExecutor = taskExecutor;
        FileDownloadLink = downloadLink;
    }

    public static implicit operator InputOnlineFile(TaskResultPreviewFromMPlus this_) => new(this_.FileDownloadLink);
}
