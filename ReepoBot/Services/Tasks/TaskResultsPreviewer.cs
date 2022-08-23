using Common;
using Newtonsoft.Json.Linq;
using ReepoBot.Models.TaskResultPreviews;
using System.Text.Json;

namespace ReepoBot.Services.Tasks;

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
        _taskRegistry.Remove(taskId, out var sessionId);
        if (sessionId is null) return null;

        string iid = (await GetTaskOutputIidAsync(taskId, sessionId)).Result;
        JsonElement mpItem;
        while (true)
        {
            mpItem = JsonDocument.Parse(
                await _httpClient.GetStringAsync($"https://tasks.microstock.plus/rphtaskmgr/getmympitem?sessionid={sessionId}&iid={iid}")
                ).RootElement;
            _logger.LogDebug("mympitem is received:\n{Json}", mpItem);

            mpItem = mpItem.GetProperty("item");
            if (mpItem.GetProperty("state").GetString() == "received")
                return new(mpItem);
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
