using Common.Tasks.Tasks;
using Common.Tasks.Tasks.DTO;
using System.Net.Http.Json;
using System.Text.Json;

namespace Node.Tasks.Client;

// It should be consolidated with NodeTask cause it's basically the same thing.
public record ClientTask(string Id, RequestOptions RequestOptions)
{
    public static async Task<ClientTask> RegisterAsync<T>(NodeTask<T> task, RequestOptions? requestOptions = null) where T : IPluginActionData<T>
    {
        requestOptions ??= new();

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
        var response = await Api.TryPostAsync(
            $"{Api.TaskManagerEndpoint}/registermytask", httpContent, requestOptions)
            .ConfigureAwait(false);
        var jsonResponse = await response.Content.ReadAsStringAsync(requestOptions.CancellationToken).ConfigureAwait(false);

        string taskId = JsonDocument.Parse(jsonResponse).RootElement.GetProperty("taskid").GetString()!;
        return new(taskId, requestOptions);
    }

    public async Task<TaskState> GetStateAsync()
    {
        var queryString = $"sessionid={Settings.SessionId}&taskid={Id}";
        var response = await Api.TryGetAsync($"{Api.TaskManagerEndpoint}/getmytaskstate?{queryString}", RequestOptions);
        return Enum.Parse<TaskState>(await response.Content.ReadAsStringAsync(), true);
    }
}
