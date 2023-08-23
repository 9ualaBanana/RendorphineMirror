namespace Node.Tasks.IO.Output;

public abstract class FileTaskUploadHandler<TData> : FileTaskUploadHandler<TData, ReadOnlyTaskFileList>
    where TData : ITaskOutputInfo
{ }

public abstract class FileTaskUploadHandler<TData, TResult> : TaskUploadHandler<TData, TResult>
    where TData : ITaskOutputInfo
    where TResult : IReadOnlyTaskFileList
{
    public override async Task UploadResult(TData info, TResult result, CancellationToken token)
    {
        result.AssertListValid("output");
        Logger.LogInformation($"Result files: {string.Join(", ", result)}");

        await base.UploadResult(info, result, token);
    }
}
