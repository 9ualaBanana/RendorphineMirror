using Telegram.MPlus.Files;

namespace Telegram.Tasks.ResultPreview;

internal abstract partial class RTaskResult
{
    internal abstract class MPlus : RTaskResult
    {
        internal MPlusFileInfo FileInfo { get; }

        internal Uri FileDownloadLink { get; }

        internal Uri PreviewDownloadLink { get; }

        internal static async Task<RTaskResult.MPlus> CreateAsync(UserExecutedRTask executedTask, MPlusFileInfo fileInfo)
        {
            var previewDownloadLink = await executedTask.GetFileDownloadLinkAsyncUsing(fileInfo.Iid, Extension.jpeg);
            var downloadLink = executedTask.Action is not TaskAction.VeeeVectorize ? previewDownloadLink : await executedTask.GetFileDownloadLinkAsyncUsing(fileInfo.Iid, Extension.eps);
            return fileInfo.Type switch
            {
                MPlusFileType.Raster or MPlusFileType.Vector => new Image(executedTask, fileInfo, downloadLink, previewDownloadLink),
                MPlusFileType.Video => new Video(executedTask, fileInfo, downloadLink, previewDownloadLink),
                _ => throw new NotImplementedException()
            };
        }

        protected MPlus(ExecutedRTask executedTask, MPlusFileInfo fileInfo, Uri fileDownloadLink, Uri previewDownloadLink)
            : base(executedTask)
        {
            FileInfo = fileInfo;
            FileDownloadLink = fileDownloadLink;
            PreviewDownloadLink = previewDownloadLink;
        }


        internal class Image : MPlus
        {
            internal int Width { get; }
            internal int Height { get; }

            internal Image(ExecutedRTask executedTask, MPlusFileInfo fileInfo, Uri fileDownloadLink, Uri previewDownloadLink)
                : base(executedTask, fileInfo, fileDownloadLink, previewDownloadLink)
            {
                var imageDimensions = fileInfo["media"]!["jpeg"]!;
                Width = (int)imageDimensions["width"]!;
                Height = (int)imageDimensions["height"]!;
            }
        }

        internal class Video : MPlus
        {
            internal int Width { get; }
            internal int Height { get; }

            internal long Mp4Size { get; }
            internal string Mp4Url { get; }

            internal long WebmSize { get; }
            internal string WebmUrl { get; }

            internal Video(ExecutedRTask executedTask, MPlusFileInfo fileInfo, Uri downloadLink, Uri previewDownloadLink)
                : base(executedTask, fileInfo, downloadLink, previewDownloadLink)
            {
                var videoInfo = fileInfo["videopreview"]!;
                Width = (int)videoInfo["width"]!;
                Height = (int)videoInfo["height"]!;

                Mp4Size = (long)videoInfo["mp4"]!["size"]!;
                Mp4Url = (string)videoInfo["mp4"]!["url"]!;

                WebmSize = (long)videoInfo["webm"]!["size"]!;
                WebmUrl = (string)videoInfo["webm"]!["url"]!;
            }
        }
    }
}
