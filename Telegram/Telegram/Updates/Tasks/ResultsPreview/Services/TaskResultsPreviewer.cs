using Newtonsoft.Json.Linq;
using Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Services;

public class TaskResultsPreviewer
{
    readonly HttpClient _httpClient;
    readonly ILogger<TaskResultsPreviewer> _logger;
    readonly TaskRegistry _taskRegistry;

    public TaskResultsPreviewer(IHttpClientFactory httpClientFactory, ILogger<TaskResultsPreviewer> logger, TaskRegistry taskRegistry)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _taskRegistry = taskRegistry;
    }

    public async Task<MpItem?> GetMyMPItemAsync(ITaskApi taskApi, string executorNodeName)
    {
        _taskRegistry.TryGetValue(taskApi.Id, out var authenticationToken);
        if (authenticationToken is null) return null;

        string iid = (await GetTaskOutputIidAsync(taskApi, authenticationToken.MPlus.SessionId)).Result;
        JToken mpItem;
        int retryAttempts = 3;
        while (true)
        {
            try
            {
                mpItem = await Api.GetJsonFromResponseIfSuccessfulAsync(
                    await _httpClient.GetAsync($"https://tasks.microstock.plus/rphtaskmgr/getmympitem?sessionid={authenticationToken.MPlus.SessionId}&iid={iid}"));
            }
            catch { if (retryAttempts-- == 0) return null; else continue; }

            mpItem = mpItem["item"]!;
            if ((string)mpItem["state"]! == "received")
            {
                _logger.LogDebug("mympitem is received:\n{Json}", mpItem);
                string downloadUri;
                try { downloadUri = (await taskApi.GetMPlusItemDownloadLinkAsync(iid, authenticationToken.MPlus.SessionId)).ThrowIfError(); }
                catch { return null; }
                return new MpItem(mpItem, executorNodeName, downloadUri);
            }
            else Thread.Sleep(2000);
        }
    }

    static async Task<OperationResult<string>> GetTaskOutputIidAsync(ITaskApi taskApi, string sessionId)
    {
        return await Apis.Default.WithSessionId(sessionId).GetTaskStateAsyncOrThrow(taskApi)
            .Next(taskinfo => ((MPlusTaskOutputInfo) taskinfo.Output).IngesterHost?.AsOpResult() ?? OperationResult.Err("Could not find ingester host"))
            .Next(ingester => Api.Default.ApiGet<string>($"https://{ingester}/content/vcupload/getiid", "iid", "Getting output iid", ("extid", taskApi.Id)))
            .ConfigureAwait(false);
    }
}
