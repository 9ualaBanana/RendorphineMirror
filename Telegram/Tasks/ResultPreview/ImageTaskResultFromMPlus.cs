using Telegram.MPlus;

namespace Telegram.Tasks.ResultPreview;

/// <inheritdoc cref="TaskResultFromMPlus"/>
internal record ImageTaskResultFromMPlus : TaskResultFromMPlus
{
    internal readonly int Width;
    internal readonly int Height;

    internal ImageTaskResultFromMPlus(MPlusFileInfo fileInfo, ExecutedTask executedTask, Uri downloadLink, Uri previewDownloadLink)
        : base(fileInfo, executedTask, downloadLink, previewDownloadLink)
    {
        var imageDimensions = fileInfo["media"]!["jpeg"]!;
        Width = (int)imageDimensions["width"]!;
        Height = (int)imageDimensions["height"]!;
    }
}
