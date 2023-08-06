namespace Node.Tasks.IO.GHandlers.Input;

public class MPlusTaskHandlerInfo : FileTaskInputHandlerInfo<MPlusTaskInputInfo>
{
    public override TaskInputType Type => TaskInputType.MPlus;
    protected override Type HandlerType => typeof(Handler);
    protected override Type TaskObjectProviderType => typeof(TaskObjectProvider);


    class Handler : FileHandlerBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required NodeCommon.Apis Api { get; init; }
        public required IComponentContext ComponentContext { get; init; }

        public override async Task<ReadOnlyTaskFileList> Download(MPlusTaskInputInfo input, TaskObject obj, CancellationToken tokenn)
        {
            var files = new TaskFileList(TaskDirectoryProvider.InputDirectory);
            var lastex = null as Exception;
            var firstaction = (IFilePluginActionInfo) ComponentContext.ResolveKeyed<IPluginActionInfo>(TaskExecutorByData.GetTaskName(((ReceivedTask) ApiTask).Info.Data));

            foreach (var inputformats in firstaction.InputFileFormats.OrderByDescending(fs => fs.Sum(f => (int) f + 1)))
            {
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
                var downloadLink = await Api.ShardGet<string>(ApiTask, "gettaskinputdownloadlink", "link", "Getting m+ input download link",
                    ("taskid", ApiTask.Id), ("format", format.ToString().ToLowerInvariant()), ("original", format == FileFormat.Jpeg ? "1" : "0"));

                using var response = await Api.Api.Client.GetAsync(downloadLink.ThrowIfError(), HttpCompletionOption.ResponseHeadersRead, token);
                using var file = File.Open(files.New(format).Path, FileMode.Create, FileAccess.Write);

                try { Logger.LogInformation($"[M+ ITH] {format} file is {response.Content.Headers.ContentLength} bytes"); }
                catch { }

                await response.Content.CopyToAsync(file, token);
            }
        }
    }
    class TaskObjectProvider : TaskObjectProviderBase
    {
        public override async Task<OperationResult<TaskObject>> GetTaskObject(MPlusTaskInputInfo input, CancellationToken token) =>
            await input.GetFileInfo(Settings.SessionId, Settings.UserId);
    }
}
