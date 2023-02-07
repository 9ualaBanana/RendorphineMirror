using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

namespace Telegram.Models;

public class MPlusService
{
    readonly HttpClient _httpClient;

    readonly ILogger<MPlusService> _logger;

    public MPlusService(HttpClient httpClient, ILogger<MPlusService> logger)
	{
		_httpClient = httpClient;
        _logger = logger;
	}

    internal async Task<MPlusFileInfo> RequestFileInfoAsync(string sessionId, string iid, CancellationToken cancellationToken)
    {
        var endpoint = new Uri(new Uri(Api.TaskManagerEndpoint), "getmympitem").ToString();
        var requestUrl = QueryHelpers.AddQueryString(endpoint, new Dictionary<string, string?> { { "sessionId", sessionId }, { "iid", iid } });

        return await RequestFileInfoAsyncCore(cancellationToken);


        async Task<MPlusFileInfo> RequestFileInfoAsyncCore(CancellationToken cancellationToken)
        {
            int attemptsLeft = 3;
            JToken mPlusFileInfo;
            while (attemptsLeft > 0)
            {
                mPlusFileInfo = (await (await _httpClient.GetAsync(requestUrl, cancellationToken)).GetJsonIfSuccessfulAsync())["item"]!;
                if ((string)mPlusFileInfo["state"]! == "received")
                    return MPlusFileInfo.From(mPlusFileInfo);
                else Thread.Sleep(TimeSpan.FromSeconds(3));
            }

            var exception = new Exception("Couldn't request {FileInfo} for the file with IID {Iid}.");
            _logger.LogError(exception, message: default);
            throw exception;
        }
    }

    internal async Task<Uri> RequestFileDownloadLinkUsing(ITaskApi taskApi, string sessionId, string iid)
        => new Uri((await taskApi.GetMPlusItemDownloadLinkAsync(iid, sessionId)).ThrowIfError());
}
