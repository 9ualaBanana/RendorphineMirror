using System.Net;

namespace Node.Tasks.IO.Handlers.Input;

public static class DownloadLink
{
    public class InputDownloader : FileTaskInputDownloader<DownloadLinkTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.DownloadLink;
        public required DataDirs Dirs { get; init; }
        public required Api Api { get; init; }

        protected override async Task<ReadOnlyTaskFileList> DownloadImpl(DownloadLinkTaskInputInfo data, TaskObject obj, CancellationToken token)
        {
            using var response = await Api.Get(data.Url);
            if (data.Url.Contains("t.microstock.plus") && response.StatusCode == HttpStatusCode.NotFound)
                throw new TaskFailedException("Got 404 when trying to get image from the tg bot");

            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException($"Download link `{data.Url}` request returned status code {response.StatusCode}", null, response.StatusCode);

            using var _ = Directories.DisposeDelete(Dirs.TempFile(), out var tempfile);
            using (var file = File.Open(tempfile, FileMode.Create, FileAccess.Write))
                await response.Content.CopyToAsync(file, token);

            var format = await tryall(
                asTask(() => FileFormatExtensions.FromMime(response.Content.Headers.ContentType!.ToString())),
                asTask(() => FileFormatExtensions.FromFilename(response.Content.Headers.ContentDisposition!.FileName!)),
                asTask(() => FileFormatExtensions.FromFilename(data.Url.Split('?')[0])),
                tryFFProbe,
                asTask(() => FileFormatExtensions.FromFilename(obj.FileName))
            );

            var files = new TaskFileList(TaskDirectoryProvider.InputDirectory);
            File.Move(tempfile, files.New(format).Path);

            return files;


            Func<Task<FileFormat>> asTask(Func<FileFormat> func) => () => Task.FromResult(func());
            async Task<FileFormat> tryFFProbe()
            {
                Logger.LogInformation("Trying to determine file type using ffprobe");

                var ffprobe = await FFProbe.Get(tempfile, Logger);
                var format = toformat(ffprobe.Streams[0]);
                Logger.LogInformation($"File format determined to be {format}");

                return format;


                FileFormat toformat(FFProbe.FFProbeStreamInfo codec)
                {
                    if (codec.CodecName.Contains("jpeg", StringComparison.Ordinal) || codec.CodecName.Contains("jpg", StringComparison.Ordinal))
                        return FileFormat.Jpeg;
                    if (codec.CodecName.Contains("png", StringComparison.Ordinal))
                        return FileFormat.Png;

                    if (codec.CodecType == "video") return FileFormat.Mov;

                    // ffmpeg cant handle vector so just fail
                    throw new Exception();
                }
            }
            async Task<FileFormat> tryall(params Func<Task<FileFormat>>[] funcs)
            {
                for (int i = 0; i < funcs.Length; i++)
                {
                    try { return await funcs[i](); }
                    catch
                    {
                        if (i == funcs.Length - 1)
                            throw;
                    }
                }

                throw new Exception("Could not find a valid file format");
            }
        }
    }
    public class TaskObjectProvider : TaskObjectProvider<DownloadLinkTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.DownloadLink;
        public required Api Api { get; init; }

        public override async Task<OperationResult<TaskObject>> GetTaskObject(DownloadLinkTaskInputInfo input, CancellationToken token)
        {
            var headers = await Api.Client.GetAsync(input.Url, HttpCompletionOption.ResponseHeadersRead, token);
            if (!headers.IsSuccessStatusCode)
                return OperationResult.Err(new HttpError(null, headers, null));

            return new TaskObject(Path.GetFileName(input.Url), headers.Content.Headers.ContentLength!.Value);
        }
    }
}