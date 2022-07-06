using Common.Tasks.Tasks;
using Common.Tasks.Tasks.DTO;
using Node.Tasks.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Node.Tasks;

public class TaskManager : IDisposable
{
    readonly HttpListener _httpListener = new();
    public RequestOptions RequestOptions { get; set; }

    public TaskManager(RequestOptions? requestOptions = null)
    {
        //_httpListener.Prefixes.Add($"http://localhost:{PortForwarding.Port}/rphtaskexec/launchtask/");
        RequestOptions = requestOptions ?? new();
    }

    // Moved to Listener.cs
    public async Task StartAcceptingTasksAsync()
    {
        try
        {
            _httpListener.Start();
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine(ex.ErrorCode);
        }
        while (_httpListener.IsListening)
        {
            var context = await _httpListener.GetContextAsync();
            var incomingTask = await JsonSerializer.DeserializeAsync<IncomingTask>(
                context.Request.InputStream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                RequestOptions.CancellationToken).ConfigureAwait(false);
            await AcceptTaskAsync(incomingTask!).ConfigureAwait(false);
        }
    }

    internal async Task AcceptTaskAsync(IncomingTask task)
    {
        TaskHandler taskHandler;
        if (task.HasMPlusInput) taskHandler = new MPlusTaskHandler(task, RequestOptions);
        else return;
        await taskHandler.HandleAsync();
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
            RequestOptions).ConfigureAwait(false);
        var jsonResponse = await response.Content.ReadAsStringAsync(RequestOptions.CancellationToken).ConfigureAwait(false);

        return JsonDocument.Parse(jsonResponse).RootElement.GetProperty("taskid").GetString()!;
    }

    public void Dispose()
    {
        _httpListener.Close();
        GC.SuppressFinalize(this);
    }
}