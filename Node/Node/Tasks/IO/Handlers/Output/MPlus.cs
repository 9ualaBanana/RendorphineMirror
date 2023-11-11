using Transport.Upload;

namespace Node.Tasks.IO.Handlers.Output;

public static class MPlus
{
    public class UploadHandler : FileTaskUploadHandler<MPlusTaskOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.MPlus;
        public required IRegisteredTaskApi ApiTask { get; init; }

        protected override async Task UploadResultImpl(MPlusTaskOutputInfo info, ITaskInputInfo input, ReadOnlyTaskFileList result, CancellationToken token)
        {
            // example: files=["input.g.jpg", "input.t.jpg"] returns "input."
            var commonprefix = result.Paths.Select(Path.GetFileNameWithoutExtension).Cast<string>().Aggregate((seed, z) => string.Join("", seed.TakeWhile((v, i) => z.Length > i && v == z[i])));
            if (commonprefix.EndsWith('.'))
                commonprefix = commonprefix[..^1];

            Logger.LogInformation($"[MPlus] Uploading {result.Count} files with common prefix '{commonprefix}': {string.Join(", ", result.Paths.Select(Path.GetFileName))}");

            foreach (var file in result.Paths)
            {
                var postfix = Path.GetFileNameWithoutExtension(file);
                if (postfix.StartsWith("output", StringComparison.Ordinal))
                    postfix = postfix.Substring("output".Length);
                else postfix = postfix.Substring(commonprefix.Length);

                Logger.LogInformation($"[MPlus] Uploading {file} with postfix '{postfix}'");
                var iid = await PacketsTransporter.UploadAsync(await MPlusTaskResultUploadSessionData.InitializeAsync(file, postfix: postfix, ApiTask, Api.GlobalClient, Settings.SessionId, (input as MPlusTaskInputInfo)?.Iid), cancellationToken: token);

                ((ReceivedTask) ApiTask).UploadedFiles.Add(new MPlusUploadedFileInfo(iid, file));
            }
        }
    }
    public class CompletionChecker : TaskCompletionChecker<MPlusTaskOutputInfo>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.MPlus;

        public override bool CheckCompletion(MPlusTaskOutputInfo info, TaskState state) =>
            state == TaskState.Validation && info.Data?.Values.All(data => data.IngesterHost is not null) == true;
    }
}
