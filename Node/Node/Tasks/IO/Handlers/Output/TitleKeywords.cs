namespace Node.Tasks.IO.Handlers.Output;

public static class TitleKeywords
{
    public class UploadHandler : TaskUploadHandler<TitleKeywordsOutputInfo, Exec.Actions.TitleKeywords>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.TitleKeywords;

        public required IRegisteredTaskApi ApiTask { get; init; }
        public required NodeCommon.Apis Api { get; init; }

        protected override async Task UploadResultImpl(TitleKeywordsOutputInfo info, Exec.Actions.TitleKeywords result, CancellationToken token)
        {
            await Api.Api.ApiPost($"{Api.TaskManagerEndpoint}/rphtasklauncher/settaskoutputtitlekeywords", "setting task output title&keywords",
                Apis.Default.AddSessionId(("taskid", ApiTask.Id), ("title", result.Title), ("keywords", JsonConvert.SerializeObject(result.Keywords)))
            ).ThrowIfError();
        }
    }
}
