using Node.Tasks.Models;
using System.Net;
using System.Text.Json;

namespace Node.Tasks.Executor;

public class TaskReceiver : IDisposable
{
    readonly HttpListener _httpListener = new();
    public RequestOptions RequestOptions { get; set; }

    public TaskReceiver(RequestOptions? requestOptions = null)
    {
        _httpListener.Prefixes.Add($"http://*:{PortForwarding.Port}/rphtaskexec/launchtask/");
        RequestOptions = requestOptions ?? new();
    }

    public async Task StartAsync()
    {
        try { _httpListener.Start(); }
        catch (HttpListenerException ex) { Console.WriteLine($"{typeof(TaskReceiver).Name} couldn't start:\n{ex}"); }

        while (_httpListener.IsListening)
        {
            var context = await _httpListener.GetContextAsync();
            var receivedTask = await JsonSerializer.DeserializeAsync<ReceivedTask>(
                context.Request.InputStream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                RequestOptions.CancellationToken).ConfigureAwait(false);
            receivedTask!.RequestOptions = RequestOptions;
            await receivedTask.HandleAsync();
        }
    }

    public void Dispose()
    {
        _httpListener.Close();
        GC.SuppressFinalize(this);
    }
}