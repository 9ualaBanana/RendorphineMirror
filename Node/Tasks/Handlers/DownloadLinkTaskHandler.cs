using System.Net;

namespace Node.Tasks.Handlers;

public class DownloadLinkTaskHandler : ITaskInputHandler
{
    TaskInputType ITaskInputHandler.Type => TaskInputType.DownloadLink;

    public async ValueTask Download(ReceivedTask task, CancellationToken cancellationToken)
    {
        var info = (DownloadLinkTaskInputInfo) task.Input;

        using var data = await Api.Default.Get(info.Url);
        if (info.Url.Contains("t.microstock.plus") && data.StatusCode == HttpStatusCode.NotFound)
            task.ThrowFailed("Got 404 when trying to get image from the reepo");

        if (data.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException($"Download link `{info.Url}` request returned status code {data.StatusCode}", null, data.StatusCode);

        var tempfile = Path.Combine(Init.TempDirectory(), Guid.NewGuid().ToString());
        using var _ = new FuncDispose(() =>
        {
            if (File.Exists(tempfile))
                File.Delete(tempfile);
        });

        using (var file = File.Open(tempfile, FileMode.Create, FileAccess.Write))
            await data.Content.CopyToAsync(file, cancellationToken);

        var format = await tryall(
            asTask(() => FileFormatExtensions.FromMime(data.Content.Headers.ContentType!.ToString())),
            asTask(() => FileFormatExtensions.FromFilename(data.Content.Headers.ContentDisposition!.FileName!)),
            asTask(() => FileFormatExtensions.FromFilename(info.Url.Split('?')[0])),
            tryFFProbe
            // TODO: ffprobe doesnt know about EPS, so do something custom
        );

        File.Move(tempfile, task.FSNewInputFile(format));


        Func<ValueTask<FileFormat>> asTask(Func<FileFormat> func) => () => ValueTask.FromResult(func());
        async ValueTask<FileFormat> tryFFProbe()
        {
            task.LogInfo("Trying to determine file type using ffprobe");

            var ffprobe = await FFMpegTasks.FFProbe.Get(tempfile, task);
            var format = toformat(ffprobe.Streams[0]);
            task.LogInfo($"File format determined to be {format}");

            return format;


            FileFormat toformat(FFMpegTasks.FFProbe.FFProbeStreamInfo codec)
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
        async ValueTask<FileFormat> tryall(params Func<ValueTask<FileFormat>>[] funcs)
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

    public async ValueTask<OperationResult<TaskObject>> GetTaskObject(ITaskInputInfo input)
    {
        var dinput = (DownloadLinkTaskInputInfo) input;

        var headers = await Api.Client.GetAsync(dinput.Url, HttpCompletionOption.ResponseHeadersRead);
        if (!headers.IsSuccessStatusCode)
            return OperationResult.Err() with { HttpData = new(headers, null) };

        return new TaskObject(Path.GetFileName(dinput.Url), headers.Content.Headers.ContentLength!.Value);
    }
}
