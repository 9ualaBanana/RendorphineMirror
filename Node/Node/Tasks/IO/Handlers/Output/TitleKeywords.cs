using Node.Tasks.Exec.Actions;

namespace Node.Tasks.IO.Handlers.Output;

public static class TitleKeywords
{
    public class UploadHandler : TaskUploadHandler<TitleKeywordsOutputInfo, TitleKeywordsOutput>, ITypedTaskOutput
    {
        public static TaskOutputType Type => TaskOutputType.TitleKeywords;

        public required IRegisteredTaskApi ApiTask { get; init; }
        public required Apis Api { get; init; }

        protected override async Task UploadResultImpl(TitleKeywordsOutputInfo info, TitleKeywordsOutput result, CancellationToken token)
        {
            var args = Api.AddSessionId(("taskid", ApiTask.Id), ("title", result.Title), ("keywords", JsonConvert.SerializeObject(result.Keywords)));
            if (result.Description is not null)
                args = args.Append(("description", result.Description)).ToArray();

            await Api.ShardPost(ApiTask, "settaskoutputtitlekeywords", "setting task output title&keywords", args).ThrowIfError();
        }
    }
}
