using Common;
using Newtonsoft.Json.Linq;
using System.Text.Json;
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

        string iid = (await GetTaskOutputIidAsync(taskId, authenticationToken.SessionId)).Result;
        JsonElement mpItem;
        while (true)
        {
            mpItem = JsonDocument.Parse(
                await _httpClient.GetStringAsync($"https://tasks.microstock.plus/rphtaskmgr/getmympitem?sessionid={authenticationToken.SessionId}&iid={iid}")
                ).RootElement;
            mpItem = mpItem.GetProperty("item");

            if (mpItem.GetProperty("state").GetString() == "received")
            { _logger.LogDebug("mympitem is received:\n{Json}", mpItem); return new(mpItem); }
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
