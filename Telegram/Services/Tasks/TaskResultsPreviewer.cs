using Common;
using Newtonsoft.Json.Linq;
using Telegram.Models.TaskResultPreviews;

namespace Telegram.Services.Tasks;

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

    public async Task<MpItem?> GetMyMPItemAsync(string taskId)
    {
        _taskRegistry.TryGetValue(taskId, out var authenticationToken);
        if (authenticationToken is null) return null;

        string iid = (await GetTaskOutputIidAsync(taskId, authenticationToken.MPlus.SessionId)).Result;
        JToken mpItem;
        while (true)
        {
            try
            {
                mpItem = await Api.GetJsonFromResponseIfSuccessfulAsync(
                    await _httpClient.GetAsync($"https://tasks.microstock.plus/rphtaskmgr/getmympitem?sessionid={authenticationToken.MPlus.SessionId}&iid={iid}"));
            }
            catch { return null; }

            mpItem = mpItem["item"]!;
            if ((string)mpItem["state"]! == "received")
            { _logger.LogDebug("mympitem is received:\n{Json}", mpItem); return new(mpItem); }
            else Thread.Sleep(1000);
        }
    }

    static async Task<OperationResult<string>> GetTaskOutputIidAsync(string taskId, string sessionId)
    {
        return await Apis.GetTaskStateAsync(taskId, sessionId)
            .Next(taskinfo => taskinfo.Output["ingesterhost"]?.Value<string>().AsOpResult() ?? OperationResult.Err("Could not find ingester host"))
            .Next(ingester => Api.ApiGet<string>($"https://{ingester}/content/vcupload/getiid", "iid", "Getting output iid", ("extid", taskId)))
            .ConfigureAwait(false);
    }
}
