using Telegram.Bot.Types.InputFiles;
using Telegram.MPlus;

namespace Telegram.Tasks.ResultPreview;

/// <summary>
/// Task result media file uploaded to M+.
/// </summary>
/// <remarks>
/// Implicitly convertible to <see cref="InputOnlineFile"/>.
/// </remarks>
internal abstract record TaskResultFromMPlus
{
    /// <inheritdoc cref="MPlusFileInfo"/>
    internal readonly MPlusFileInfo FileInfo;

    /// <summary>
    /// ID of the task that was responsible for producing this <see cref="TaskResultFromMPlus"/>.
    /// </summary>
    internal readonly string Id;

    internal readonly TaskAction Action;

    /// <summary>
    /// Name of the node that was responsible for producing this <see cref="TaskResultFromMPlus"/>.
    /// </summary>
    internal readonly string Executor;

    internal readonly Uri FileDownloadLink;

    internal readonly Uri PreviewDownloadLink;

    /// <summary>
    /// Static factory for constructing <see cref="TaskResultFromMPlus"/> instances.
    /// </summary>
    internal static TaskResultFromMPlus Create(
        ExecutedTask executedTask,
        MPlusFileInfo fileInfo,
        Uri downloadLink,
        Uri previewDownloadLink) => fileInfo.Type switch
        {
            MPlusFileType.Raster or MPlusFileType.Vector => new ImageTaskResultFromMPlus(fileInfo, executedTask, downloadLink, previewDownloadLink),
            MPlusFileType.Video => new VideoTaskResultFromMPlus(fileInfo, executedTask, downloadLink, previewDownloadLink),
            _ => throw new NotImplementedException()
        };

    protected TaskResultFromMPlus(MPlusFileInfo fileInfo, ExecutedTask executedTask, Uri downloadLink, Uri previewDownloadLink)
    {
        FileInfo = fileInfo;
        Id = (string)FileInfo["extid"]!;
        Action = executedTask.Action;
        Executor = executedTask.Executor;
        FileDownloadLink = downloadLink;
        PreviewDownloadLink = previewDownloadLink;
    }

    public static implicit operator InputOnlineFile(TaskResultFromMPlus this_) => new(this_.PreviewDownloadLink);
}
