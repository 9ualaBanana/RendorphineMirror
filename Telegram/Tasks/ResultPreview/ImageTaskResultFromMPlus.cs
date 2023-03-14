using Telegram.MPlus;

namespace Telegram.Tasks.ResultPreview;

/// <inheritdoc cref="TaskResultFromMPlus"/>
internal record ImageTaskResultFromMPlus : TaskResultFromMPlus
{
    internal readonly int Width;
    internal readonly int Height;

    internal ImageTaskResultFromMPlus(MPlusFileInfo mPlusFileInfo, string taskType, string taskExecutor, Uri downloadLink)
        : base(mPlusFileInfo, taskType, taskExecutor, downloadLink)
    {
        var imageDimensions = mPlusFileInfo["media"]!["jpeg"]!;
        Width = (int)imageDimensions["width"]!;
        Height = (int)imageDimensions["height"]!;
    }
}
