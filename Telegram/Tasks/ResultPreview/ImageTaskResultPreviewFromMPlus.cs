using Telegram.MPlus;

namespace Telegram.Tasks.ResultPreview;

/// <inheritdoc cref="TaskResultPreviewFromMPlus"/>
internal record ImageTaskResultPreviewFromMPlus : TaskResultPreviewFromMPlus
{
    internal readonly int Width;
    internal readonly int Height;

    internal ImageTaskResultPreviewFromMPlus(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
        : base(mPlusFileInfo, taskExecutor, downloadLink)
    {
        var imageDimensions = mPlusFileInfo["media"]!["jpeg"]!;
        Width = (int)imageDimensions["width"]!;
        Height = (int)imageDimensions["height"]!;
    }
}
