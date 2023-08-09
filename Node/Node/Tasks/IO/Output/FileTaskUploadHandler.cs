namespace Node.Tasks.IO.Output;

public abstract class FileTaskUploadHandler<TData> : TaskUploadHandler<TData, ReadOnlyTaskFileList>
    where TData : ITaskOutputInfo
{
    public override async Task UploadResult(TData info, ReadOnlyTaskFileList result, CancellationToken token)
    {
        result.AssertListValid("output");
        Logger.LogInformation($"Result files: {string.Join(", ", result)}");

        await base.UploadResult(info, result, token);
    }
}
