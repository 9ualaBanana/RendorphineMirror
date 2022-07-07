using Node.P2P;
using Node.Tasks.Models;
using System.Text.Json;

namespace Node.Tasks.Executor;

internal class MPlusTaskHandler : TaskHandler
{
    readonly string _fileName = Guid.NewGuid().ToString();

    internal MPlusTaskHandler(ReceivedTask task, RequestOptions requestOptions)
        : base(task, requestOptions)
    {
    }

    protected override async Task<FileInfo> ReceiveInputAsync()
    {
        var downloadLink = await GetDownloadLinkAsync().ConfigureAwait(false);
        using var inputStream = await GetInputStream(downloadLink);
        using var file = File.OpenWrite(_fileName);
        await inputStream.CopyToAsync(file, RequestOptions.CancellationToken);
        return new(_fileName);
    }

    async Task<string> GetDownloadLinkAsync()
    {
        var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["taskid"] = Task.Id,
            ["format"] = "mov",
            ["original"] = "true"
        });
        var rawJsonResponse = await (await Api.TryPostAsync(
            $"{Api.TaskManagerEndpoint}/gettaskinputdownloadlink",
            httpContent,
            RequestOptions)
        ).Content.ReadAsStringAsync(RequestOptions.CancellationToken);
        return JsonDocument.Parse(rawJsonResponse).RootElement.GetProperty("link").GetString()!;
    }

    async Task<Stream> GetInputStream(string downloadLink)
    {
        return await
            (await Api.TryGetAsync(downloadLink, RequestOptions))
            .Content.ReadAsStreamAsync(RequestOptions.CancellationToken);
    }

    protected override Task<FileInfo> HandleAsyncCore(FileInfo input)
    {
        throw new NotImplementedException();
    }

    protected override async Task OutputResultAsync(FileInfo output)
    {
        var packetsTransporter = new PacketsTransporter(RequestOptions);
        await packetsTransporter.UploadAsync(output, Task.Id);
    }
}
