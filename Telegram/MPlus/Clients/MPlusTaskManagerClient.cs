using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using Telegram.MPlus.Files;
using Telegram.MPlus.Security;

namespace Telegram.MPlus.Clients;

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
    public async Task<MPlusIdentity> AuthenticateAsyncUsing(string email, string password)
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

    public async Task<MPlusFileInfo> RequestFileInfoAsyncUsing(MPlusFileAccessor fileAccessor, CancellationToken cancellationToken)
    {
        var requestUrl = QueryHelpers.AddQueryString("getmympitem", new Dictionary<string, string?>()
        {
            ["sessionid"] = fileAccessor.SessionId,
            ["iid"] = fileAccessor.Iid
        });

        return await RequestFileInfoAsyncCore();


        async Task<MPlusFileInfo> RequestFileInfoAsyncCore()
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

            var exception = new Exception($"IID {fileAccessor.Iid}: {nameof(MPlusFileInfo)} request failed.");
            _logger.LogError(exception, message: default);
            throw exception;
        }
    }

    public async Task<MPlusPublicSessionInfo> GetPublicSessionInfoAsync(string sessionId, CancellationToken cancellationToken)
    {
        var requestUri = QueryHelpers.AddQueryString("checkmysession", "sessionid", sessionId);

        if (await TryGetPublicSessionInfoAsync() is MPlusPublicSessionInfo publicSessionInfo)
            return publicSessionInfo;
        else
        {
            var exception = new InvalidDataException("Public session info request returned data in an unknow format.");
            _logger.LogCritical(exception, "Public session info request failed.");
            throw exception;
        }


        async Task<MPlusPublicSessionInfo?> TryGetPublicSessionInfoAsync()
        {
            try
            {
                return (await (await _httpClient.GetAsync(requestUri, cancellationToken)).GetJsonIfSuccessfulAsync())
                    ["session"]!.ToObject<MPlusPublicSessionInfo>();
            }
            catch (Exception ex)
            {
                var exception = new HttpRequestException($"{nameof(MPlusPublicSessionInfo)} request failed.", ex);
                _logger.LogCritical(exception, exception.Message);
                throw exception;
            }
        }
    }
}
