using ReepoBot.Models.TaskResultPreviews;
using System.Text.Json;

namespace ReepoBot.Services.Tasks;

public class TaskResultsPreviewer
{
    readonly HttpClient _httpClient;
    readonly ILogger<TaskResultsPreviewer> _logger;

    public TaskResultsPreviewer(IHttpClientFactory httpClientFactory, ILogger<TaskResultsPreviewer> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<MpItem> GetMyMPItemAsync(string sessionId, string iid)
    {
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
}
