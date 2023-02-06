namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

internal record ImageTaskResultPreview : TaskResultPreview
{
    internal readonly int Width;
    internal readonly int Height;

    internal ImageTaskResultPreview(MPlusFileInfo mPlusFileInfo, string taskExecutor, Uri downloadLink)
        : base(mPlusFileInfo, taskExecutor, downloadLink)
    {
        var imageDimensions = mPlusFileInfo["media"]!["jpeg"]!;
        Width = (int)imageDimensions["width"]!;
        Height = (int)imageDimensions["height"]!;
    }
}
