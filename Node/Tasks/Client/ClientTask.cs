using System.Net.Http.Json;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Tasks.Client;

// It should be consolidated with NodeTask cause it's basically the same thing.
public record ClientTask(string Id, RequestOptions RequestOptions)
{
    public static async Task<ClientTask> RegisterAsync(JObject data, TaskObject taskobj, JObject input, JObject output, RequestOptions? requestOptions = null)
    {
        requestOptions ??= new();

        var httpContent = new MultipartFormDataContent()
        {
            { new StringContent(Settings.SessionId!), "sessionid" },
            { JsonContent.Create(taskobj, options: new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = LowercaseNamingPolicy.Instance }), "object" },
            // Cast to object is necessary to allow serialization of properties of derived classes.
            { new StringContent(input.ToString(Formatting.None)), "input" },
            { new StringContent(output.ToString(Formatting.None)), "output" },
            { new StringContent(data.ToString(Formatting.None)), "data" },
            { new StringContent(string.Empty), "origin" }
        };

        File.WriteAllText("/tmp/z", httpContent.ReadAsStringAsync().Result);

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


    class LowercaseNamingPolicy : JsonNamingPolicy
    {
        public static LowercaseNamingPolicy Instance = new();

        private LowercaseNamingPolicy() { }

        public override string ConvertName(string name) => name.ToLowerInvariant();
    }
}
