namespace Node.Tasks.IO.Handlers.Input;

public static class MPlus
{
    public class InputDownloader : FileTaskInputDownloader<MPlusTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.MPlus;

        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }
        public required IComponentContext ComponentContext { get; init; }
        public required ParallelDownloader ParallelDownloader { get; init; }

        protected override async Task<ReadOnlyTaskFileList> DownloadImpl(MPlusTaskInputInfo input, TaskObject obj, CancellationToken tokenn)
        {
            var files = new TaskFileList(TaskDirectoryProvider.InputDirectory);
            var lastex = null as Exception;
            var firstaction = (IFilePluginActionInfo) ComponentContext.ResolveKeyed<IPluginActionInfo>(TaskExecutorByData.GetTaskName(((ReceivedTask) ApiTask).Info.Data));

            var formatUrls = new Dictionary<FileFormat, string>();

            foreach (var inputformats in firstaction.InputFileFormats.OrderByDescending(fs => fs.Sum(f => (int) f + 1)))
            {
                if (inputformats.Count == 0)
                    return files;

                using var token = new CancellationTokenSource();
                Logger.LogInformation($"[M+ ITH] (Re)trying to download {string.Join(", ", inputformats)}");

                try
                {
                    await Task.WhenAll(inputformats.Select(f => download(f, CancellationTokenSource.CreateLinkedTokenSource(tokenn, token.Token).Token)));
                    return files;
                }
                catch (Exception ex)
                {
                    lastex = ex;
                    files.Clear();
                    token.Cancel();
                }
            }

            throw new TaskFailedException("Could not download any input files") { FullError = lastex?.Message };


            async Task download(FileFormat format, CancellationToken token)
            {
                while (true)
                {
                    var link = null as string;
                    lock (formatUrls)
                        formatUrls.TryGetValue(format, out link);

                    if (link is null)
                    {
                        var downloadLink = await Api.ShardGet<string>(ApiTask, "gettaskinputdownloadlink", "link", "Getting m+ input download link",
                            ("taskid", ApiTask.Id), ("format", format.ToString().ToLowerInvariant()), ("original", format == FileFormat.Jpeg ? "1" : "0"), ("iid", input.Iid));

                        if (!downloadLink && downloadLink.Error is HttpError { ErrorCode: -72 or -71 }) // There is no such content item
                            downloadLink.ThrowIfError();

                        if (!downloadLink && downloadLink.Error is HttpError { ErrorCode: -4 }) // internal network error
                        {
                            await Task.Delay(1000, token);
                            continue;
                        }

                        link = downloadLink.ThrowIfError();

                        lock (formatUrls)
                            formatUrls[format] = link;
                    }


                    var fwf = files.New(format);
                    try
                    {
                        using var file = File.Open(fwf.Path, FileMode.Create, FileAccess.Write);
                        await ParallelDownloader.Download(new Uri(link), file, token);
                    }
                    catch (Exception ex)
                    {
                        try { File.Delete(fwf.Path); }
                        catch { }

                        Logger.Error(ex, "Error while downloading a file");
                        files.Remove(fwf);
                        throw;
                    }

                    break;
                }
            }
        }
    }
    public class TaskObjectProvider : TaskObjectProvider<MPlusTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.MPlus;
        public required Api Api { get; init; }

        public override async Task<OperationResult<TaskObject>> GetTaskObject(MPlusTaskInputInfo input, CancellationToken token) =>
            await input.GetFileInfo(Api, Settings.SessionId, Settings.UserId);
    }
}
