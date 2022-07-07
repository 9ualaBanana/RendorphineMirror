using System.Text.Json;
using Common.Tasks;
using Common.Tasks.Models;
using Node.P2P;

namespace Node.Tasks;

internal class MPlusTaskHandler : TaskHandler
{
    readonly string _fileName = Guid.NewGuid().ToString();

    internal MPlusTaskHandler(IncomingTask task, RequestOptions requestOptions)
        : base(task, requestOptions)
    {
    }

    protected override async Task<string> ReceiveInputAsync()
    {
        var downloadLink = await GetDownloadLinkAsync().ConfigureAwait(false);
        using var inputStream = await GetInputStream(downloadLink);
        using var file = File.OpenWrite(_fileName);
        await inputStream.CopyToAsync(file, RequestOptions.CancellationToken);
        return _fileName;
    }

    async Task<string> GetDownloadLinkAsync()
    {
        var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["taskid"] = Task.TaskId,
            ["format"] = "mov",
            ["original"] = "true"
        });
        var rawJsonResponse = await (await Api.TrySendRequestAsync(
            async () => await RequestOptions.HttpClient.PostAsync(
                $"{Api.TaskManagerEndpoint}/gettaskinputdownloadlink",
                httpContent),
        RequestOptions)).Content.ReadAsStringAsync(RequestOptions.CancellationToken);
        return JsonDocument.Parse(rawJsonResponse).RootElement.GetProperty("link").GetString()!;
    }

    async Task<Stream> GetInputStream(string downloadLink)
    {
        return await (await Api.TrySendRequestAsync(
            async () => await RequestOptions.HttpClient.GetAsync(downloadLink, RequestOptions.CancellationToken),
            RequestOptions)
        ).Content.ReadAsStreamAsync(RequestOptions.CancellationToken);
    }

    protected override async Task<string> HandleAsyncCore(string input)
    {
        var type = Task.Task.Data.GetProperty("type").GetString();
        if (type is null) throw new InvalidOperationException("Task type is null");

        var action = TaskList.TryGet(type);
        if (action is null) throw new InvalidOperationException("Got unknown task type");

        return await action.Execute(Task, input).ConfigureAwait(false);
    }

    protected override async Task OutputResultAsync(string output)
    {
        // Session initializer should take the endpoint to which it will upload the output.
        var packetsTransporter = new PacketsTransporter(RequestOptions);
        await packetsTransporter.UploadAsync(output, Task.TaskId);
    }
}
