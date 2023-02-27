using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using Telegram.Security;
using Telegram.Tasks;

namespace Telegram.Models;

public class MPlusClient
{
    readonly HttpClient _httpClient;
    readonly static Uri _taskManagerEndpoint = new(Api.TaskManagerEndpoint+'/');

    readonly ILogger<MPlusClient> _logger;

    public MPlusClient(HttpClient httpClient, ILogger<MPlusClient> logger)
	{
		_httpClient = httpClient;
        _logger = logger;
	}

    internal async Task<MPlusFileInfo> RequestFileInfoAsync(string sessionId, string iid, CancellationToken cancellationToken)
    {
        string endpoint = new Uri(_taskManagerEndpoint, "getmympitem").ToString();
        string requestUrl = QueryHelpers.AddQueryString(endpoint, new Dictionary<string, string?> { { "sessionid", sessionId }, { "iid", iid } });

        return await RequestFileInfoAsyncCore(cancellationToken);


        async Task<MPlusFileInfo> RequestFileInfoAsyncCore(CancellationToken cancellationToken)
        {
            int attemptsLeft = 3;
            JToken mPlusFileInfoJson;
            while (attemptsLeft > 0)
            {
                mPlusFileInfoJson = (await (await _httpClient.GetAsync(requestUrl, cancellationToken)).GetJsonIfSuccessfulAsync())["item"]!;
                if ((string)mPlusFileInfoJson["state"]! == "received")
                {
                    var mPlusFileInfo = MPlusFileInfo.From(mPlusFileInfoJson);
                    _logger.LogTrace("IID {Iid}: {MPlusFileInfo} is received", mPlusFileInfo.Iid, nameof(MPlusFileInfo));
                    return mPlusFileInfo;
                }
                else Thread.Sleep(TimeSpan.FromSeconds(3));
            }

            var exception = new Exception($"IID {iid}: {nameof(MPlusFileInfo)} request failed.");
            _logger.LogError(exception, message: default);
            throw exception;
        }
    }

    internal async Task<Uri> RequestFileDownloadLinkUsing(ITaskApi taskApi, string sessionId, string iid)
        => new Uri((await taskApi.GetMPlusItemDownloadLinkAsync(iid, sessionId)).ThrowIfError());

    internal async Task<MPlusIdentity?> TryLogInAsyncUsing(string email, string password)
    {
        var credentialsForm = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["email"] = email,
            ["password"] = password,
            ["guid"] = Guid.NewGuid().ToString()
        });
        return (await (await _httpClient.PostAsync(new Uri(_taskManagerEndpoint, "login"), credentialsForm)).GetJsonIfSuccessfulAsync())
            .ToObject<MPlusIdentity>();
    }
}
