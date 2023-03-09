using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;

namespace Telegram.MPlus;

public class MPlusTaskManagerClient
{
    readonly HttpClient _httpClient;

    readonly ILogger _logger;

    public MPlusTaskManagerClient(HttpClient httpClient, ILogger<MPlusTaskManagerClient> logger)
    {
        httpClient.BaseAddress = new($"{Api.TaskManagerEndpoint}/");
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <exception cref="InvalidDataException">M+ server returned authentication result in a wrong format.</exception>
    /// <exception cref="HttpRequestException">Exception occured on the M+ server.</exception>
    internal async Task<MPlusIdentity> LogInAsyncUsing(string email, string password)
    {
        var credentialsForm = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["email"] = email,
            ["password"] = password,
            ["guid"] = Guid.NewGuid().ToString()
        });
        return (await (await _httpClient.PostAsync("login", credentialsForm)).GetJsonIfSuccessfulAsync())
            .ToObject<MPlusIdentity>() ??
            throw new InvalidDataException("M+ authentication result returned from the server was in a wrong format.");
    }

    internal async Task<MPlusFileInfo> RequestFileInfoAsyncFor(MPlusMediaFile mPlusMediaFile, CancellationToken cancellationToken)
    {
        var requestUrl = QueryHelpers.AddQueryString("getmympitem", new Dictionary<string, string?>()
        {
            ["sessionid"] = mPlusMediaFile.SessionId,
            ["iid"] = mPlusMediaFile.Iid
        });

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

            var exception = new Exception($"IID {mPlusMediaFile.Iid}: {nameof(MPlusFileInfo)} request failed.");
            _logger.LogError(exception, message: default);
            throw exception;
        }
    }
}
