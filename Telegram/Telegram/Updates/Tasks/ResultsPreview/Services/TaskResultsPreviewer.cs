using Newtonsoft.Json.Linq;
using Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Services;

public class TaskResultsPreviewer
{
    readonly TaskResultPreviewService _taskResultPreviewService;
    readonly TaskRegistry _taskRegistry;

    readonly ILogger _logger;

    public TaskResultsPreviewer(TaskResultPreviewService taskResultPreviewService, TaskRegistry taskRegistry, ILogger<TaskResultsPreviewer> logger)
    {
        _taskResultPreviewService = taskResultPreviewService;
        _taskRegistry = taskRegistry;
        _logger = logger;
    }

    public async Task<MPlusFileInfo?> GetMyMPItemAsync(ITaskApi taskApi, string executorNodeName)
    {
        //_taskRegistry.TryGetValue(taskApi.Id, out var authenticationToken);
        //if (authenticationToken is null) return null;

        ////string iid = (await GetTaskOutputIidAsync(taskApi, authenticationToken.MPlus.SessionId)).Result;
        //JToken mpItem;
        //int retryAttempts = 3;
        //while (true)
        //{
        //    try
        //    {
        //        mpItem = await Api.GetJsonFromResponseIfSuccessfulAsync(
        //            await _taskResultPreviewService.GetAsync($"https://tasks.microstock.plus/rphtaskmgr/getmympitem?sessionid={authenticationToken.MPlus.SessionId}&iid={iid}"));
        //    }
        //    catch { if (retryAttempts-- == 0) return null; else continue; }

        //    mpItem = mpItem["item"]!;
        //    if ((string)mpItem["state"]! == "received")
        //    {
        //        _logger.LogDebug("mympitem is received:\n{Json}", mpItem);
        //        string downloadUri;
        //        try { downloadUri = (await taskApi.GetMPlusItemDownloadLinkAsync(iid, authenticationToken.MPlus.SessionId)).ThrowIfError(); }
        //        catch { return null; }
        //        return new MpItem(mpItem, downloadUri);
        //    }
        //    else Thread.Sleep(2000);
        //}
        return null;
    }

    //static async Task<OperationResult<string>> GetTaskOutputIidAsync(ITaskApi taskApi, string sessionId)
    //{
    //    return await Apis.Default.WithSessionId(sessionId).GetTaskStateAsyncOrThrow(taskApi)
    //        .Next(taskinfo => ((MPlusTaskOutputInfo) taskinfo.Output).IngesterHost?.AsOpResult() ?? OperationResult.Err("Could not find ingester host"))
    //        .Next(ingester => Api.Default.ApiGet<string>($"https://{ingester}/content/vcupload/getiid", "iid", "Getting output iid", ("extid", taskApi.Id)))
    //        .ConfigureAwait(false);
    //}
}
