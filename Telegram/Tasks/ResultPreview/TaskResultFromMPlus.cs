﻿using Telegram.Bot.Types.InputFiles;
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
    /// ID of the task that was responsible for producing the task result that this <see cref="TaskResultFromMPlus"/> represents.
    /// </summary>
    internal readonly string Id;

    internal readonly string Type;

    /// <summary>
    /// Name of the node that was responsible for producing the task result that this <see cref="TaskResultFromMPlus"/> represents.
    /// </summary>
    internal readonly string Executor;

    internal readonly Uri FileDownloadLink;

    /// <summary>
    /// Static factory for constructing <see cref="TaskResultFromMPlus"/> instances.
    /// </summary>
    internal static TaskResultFromMPlus Create(MPlusFileInfo mPlusFileInfo, string taskType, string taskExecutor, Uri downloadLink)
        => mPlusFileInfo.Type switch
        {
            MPlusFileType.Raster or MPlusFileType.Vector => new ImageTaskResultFromMPlus(mPlusFileInfo, taskType, taskExecutor, downloadLink),
            MPlusFileType.Video => new VideoTaskResultFromMPlus(mPlusFileInfo, taskType, taskExecutor, downloadLink),
            _ => throw new NotImplementedException()
        };

    protected TaskResultFromMPlus(MPlusFileInfo mPlusFileInfo, string taskType, string taskExecutor, Uri downloadLink)
    {
        FileInfo = mPlusFileInfo;
        Id = (string)FileInfo["extid"]!;
        Type = taskType;
        Executor = taskExecutor;
        FileDownloadLink = downloadLink;
    }

    public static implicit operator InputOnlineFile(TaskResultFromMPlus this_) => new(this_.FileDownloadLink);
}
