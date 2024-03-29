using System.Collections.Concurrent;

namespace Node.Tasks.IO.Handlers.Input;

public static class MPlus
{
    public class InputDownloader : FileTaskInputDownloader<MPlusTaskInputInfo>, ITypedTaskInput
    {
        public static TaskInputType Type => TaskInputType.MPlus;

        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }
        public required ILifetimeScope ComponentContext { get; init; }

        class MinProgressSetter : ITaskProgressSetter
        {
            readonly ITaskProgressSetter ProgressSetter;
            readonly ConcurrentDictionary<ITaskProgressSetter, double> Progresses;
            readonly int Index, AllCount;

            public MinProgressSetter(ITaskProgressSetter progressSetter, ConcurrentDictionary<ITaskProgressSetter, double> progresses, int index, int allCount)
            {
                ProgressSetter = progressSetter;
                Progresses = progresses;
                Index = index;
                AllCount = allCount;
            }

            public void Set(double progress)
            {
                lock (Progresses)
                {
                    Progresses.TryRemove(this, out _);
                    Progresses.TryAdd(this, progress);

                    ProgressSetter.Set(Progresses.Values.Sum() / Progresses.Count);
                }
            }
        }
        protected override async Task<ReadOnlyTaskFileList> DownloadImpl(MPlusTaskInputInfo input, TaskObject obj, CancellationToken tokenn)
        {
            var files = new TaskFileList(TaskDirectoryProvider.InputDirectory);
            var lastex = null as Exception;
            var firstaction = (IFilePluginActionInfo) ComponentContext.ResolveKeyed<IPluginActionInfo>(TaskExecutorByData.GetTaskName(((ReceivedTask) ApiTask).Info.Data));
            var progressbag = new ConcurrentDictionary<ITaskProgressSetter, double>();

            var formatUrls = new Dictionary<FileFormat, string>();

            foreach (var (i, inputformats) in firstaction.InputFileFormats.OrderByDescending(fs => fs.Sum(f => (int) f + 1)).Select((x, i) => (i, x)))
            {
                if (inputformats.Count == 0)
                    return files;

                using var token = new CancellationTokenSource();
                Logger.LogInformation($"[M+ ITH] (Re)trying to download {string.Join(", ", inputformats)}");

                try
                {
                    await Task.WhenAll(inputformats.Select(f => download(f, i, inputformats.Count, CancellationTokenSource.CreateLinkedTokenSource(tokenn, token.Token).Token)));
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


            async Task download(FileFormat format, int index, int allcount, CancellationToken token)
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

                        using var scope = ComponentContext.BeginLifetimeScope(builder =>
                            builder.RegisterDecorator<ITaskProgressSetter>((ctx, parameters, instance) => new MinProgressSetter(instance, progressbag, index, allcount))
                        );
                        await scope.Resolve<ParallelDownloader>().Download(new Uri(link), file, token);
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
