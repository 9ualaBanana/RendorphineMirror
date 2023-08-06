using Node.Tasks.Exec.Actions;

namespace Node.Tasks.IO.GHandlers.Output;

public class TitleKeywordsTaskHandlerInfo : TaskOutputHandlerInfo<TitleKeywordsOutputInfo, TitleKeywords>
{
    public override TaskOutputType Type => TaskOutputType.TitleKeywords;
    protected override Type UploadHandlerType => throw new NotImplementedException();

    class UploadHandler : UploadHandlerBase
    {
        public required IRegisteredTaskApi ApiTask { get; init; }
        public required NodeCommon.Apis Api { get; init; }

        public override async Task UploadResult(TitleKeywordsOutputInfo info, TitleKeywords result, CancellationToken token)
        {
            await Api.Api.ApiPost($"{Api.TaskManagerEndpoint}/rphtasklauncher/settaskoutputtitlekeywords", "setting task output title&keywords",
                Apis.Default.AddSessionId(("taskid", ApiTask.Id), ("title", result.Title), ("keywords", JsonConvert.SerializeObject(result.Keywords)))
            ).ThrowIfError();
        }
    }
}
