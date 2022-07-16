using System.Text.Json;

namespace ReepoBot.Services.Tasks;

public class TaskResultsPreviewer
{
    readonly HttpClient _httpClient;
    public TaskResultsPreviewer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<JsonElement> GetMyMPItemAsync(string sessionId, string iid)
    {
        JsonElement mpItem;
        while (true)
        {
            mpItem = JsonDocument.Parse(
                await _httpClient.GetStringAsync($"https://tasks.microstock.plus/rphtaskmgr/getmympitem?sessionid={sessionId}&iid={iid}")
                ).RootElement;

            if (mpItem.GetProperty("state").GetString() == "received")
                return mpItem;
        }
    }
}
