using Common.Tasks.Tasks;
using Common.Tasks.Tasks.DTO;
using System.Net.Http.Json;
using System.Text.Json;

namespace Node.Tasks;

public class TaskManager
{
    public RequestOptions RequestOptions { get; set; }

    public TaskManager(RequestOptions? requestOptions = null)
    {
        RequestOptions = requestOptions ?? new();
    }

    public async Task<string> RegisterTaskAsync<T>(NodeTask<T> task) where T : IPluginActionData<T>
    {
        var httpContent = new MultipartFormDataContent()
        {
            { new StringContent(Settings.SessionId!), "sessionid" },
            { JsonContent.Create(new { filename = task.File.Name, size = task.File.Length }), "object" },
            // Cast to object is necessary to allow serialization of properties of derived classes.
            { JsonContent.Create((object)task.Input, options: new(JsonSerializerDefaults.Web)), "input" },
            { JsonContent.Create((object)task.Output, options: new(JsonSerializerDefaults.Web)), "output" },
            { JsonContent.Create(task.Data, options: new(JsonSerializerDefaults.Web) { IncludeFields = true }), "data" },
            { new StringContent(string.Empty), "origin" }
        };
        var response = await Api.TrySendRequestAsync(
            () => RequestOptions.HttpClient.PostAsync($"{Api.TaskManagerEndpoint}/registermytask", httpContent),
            RequestOptions);
        var jsonResponse = await response.Content.ReadAsStringAsync(RequestOptions.CancellationToken);

        return JsonDocument.Parse(jsonResponse).RootElement.GetProperty("taskid").GetString()!;
    }
}