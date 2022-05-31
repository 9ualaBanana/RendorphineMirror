using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public class Api
    {
        const string ApiVersion = "0.1";
        const string Url = "https://accounts.stocksubmitter.com/api/" + ApiVersion + "/";

        public event Action OnLoginError = delegate { };
        readonly HttpClient Client = new HttpClient();

        public ValueTask<OperationResult<string>> GenerateUniqueKey(string sid, CancellationToken token) =>
            Send<string>(
                true,
                "common/toolbox/generateuniquekeyforuser",
                sid,
                Enumerable.Empty<KeyValuePair<string, string>>(),
                "uniqueKey",
                token
            );


        public ValueTask<OperationResult<UserInfo>> GetUserInfo(string sid, CancellationToken token) =>
            Fetch<UserInfo>("contentdb/users/getinfo", sid, "info", token);

        ValueTask<OperationResult<T>> Fetch<T>(string urladd, string sid, string valueName, string value, string responseJsonName, CancellationToken token) =>
            Send<T>(true, urladd, sid, valueName, value, responseJsonName, token);
        ValueTask<OperationResult<T>> Fetch<T>(string urladd, string sid, string responseJsonName, CancellationToken token) =>
            Send<T>(true, urladd, sid, Enumerable.Empty<KeyValuePair<string, string>>(), responseJsonName, token);
        ValueTask<OperationResult<T>> Post<T>(string urladd, string sid, string valueName, string value, string responseJsonName, CancellationToken token) =>
            Send<T>(false, urladd, sid, valueName, value, responseJsonName, token);
        ValueTask<OperationResult<T>> Send<T>(bool get, string urladd, string sid, string valueName, string value, string responseJsonName, CancellationToken token) =>
            Send<T>(get, urladd, sid, new[] { KeyValuePair.Create(valueName, value) }, responseJsonName, token);
        async ValueTask<OperationResult<T>> Send<T>(bool get, string urladd, string sid, IEnumerable<KeyValuePair<string, string>> parameters,
            string responseJsonName, CancellationToken token)
        {
            var response = await Send(get, urladd, sid, parameters, token).ConfigureAwait(false);
            if (!response) return response.GetResult();

            // debug
            if (false && Debugger.IsAttached && Directory.Exists("/tmp"))
                WriteTempFile("/tmp", urladd, response.Result);

            try
            {
                var j = response.Result[responseJsonName];
                if (j is null) return default!;

                return j.ToObject<T>()!;
            }
            catch (Exception ex) { return OperationResult.Err(ex); }
        }
        async ValueTask<OperationResult<JObject>> Send(bool get, string urladd, string sid, IEnumerable<KeyValuePair<string, string>> parameters, CancellationToken token)
        {
            parameters = parameters.Append(KeyValuePair.Create("sid", sid)).ToArray();

            _ = Task.Run(() => Logger.Log((get ? "GET " : "POST ") + urladd + "  " + string.Join('&', parameters.Select(x => x.Key + "=" + x.Value)), false));

            var response = await
                (get
                ? SendGetRequestAsync(Url + urladd, parameters, token)
                : SendPostRequestAsync(Url + urladd, parameters, token));

            _ = Task.Run(() => Logger.Log("RET  " + (response is { Success: true } ? response.Result.ToString(Formatting.None) : response.Message), false, false));
            return response;
        }


        public async Task<OperationResult<(string url, TaskCompletionSource<OperationResult<LoginResult>> task)>> GetExternalAuthLink(LoginType type, CancellationToken token)
        {
            var keyResp = await SendGetRequestAsync(Url + "auth/external/request", new[] { KeyValuePair.Create("service", "syncer") }, token).ConfigureAwait(false);
            if (!keyResp) return keyResp.GetResult();

            var key = keyResp.Result["key"]?.Value<string>() ?? throw new NullReferenceException();
            var url = Url + "auth/external/choose?key=" + key + "&service=" + type.ToString().ToLowerInvariant();
            var task = new TaskCompletionSource<OperationResult<LoginResult>>();

            _ = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        await Task.Delay(3_000, token).ConfigureAwait(false);

                        var infoResp = await SendGetRequestAsync(Url + "auth/external/check", new[] { KeyValuePair.Create("key", key) }, token).ConfigureAwait(false);
                        if (!infoResp) task.SetResult(infoResp.GetResult());

                        var completed = infoResp.Result["isCompleted"]?.Value<bool>() ?? throw new NullReferenceException();
                        if (!completed) continue;

                        var sid = infoResp.Result["sid"]?.Value<string>() ?? throw new NullReferenceException();
                        var uid = infoResp.Result["userid"]?.Value<string>() ?? throw new NullReferenceException();
                        var email = infoResp.Result["email"]?.Value<string>() ?? throw new NullReferenceException();

                        var result = new LoginResult(email, uid, sid);
                        Logger.Log("Successful login: " + result);
                        task.SetResult(result);

                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is NullReferenceException) task.TrySetResult(OperationResult.Err("Unknown server error (NRE)"));
                    else task.TrySetResult(OperationResult.Err(ex));
                }
            });

            return (url, task);
        }
        public async Task<OperationResult<LoginResult>> AuthenticateExternalAsync(LoginType type, CancellationToken token)
        {
            var linkres = await GetExternalAuthLink(type, token).ConfigureAwait(false);
            if (!linkres) return linkres.GetResult();

            Process.Start(new ProcessStartInfo(linkres.Result.url) { UseShellExecute = true });
            return await linkres.Result.task.Task.ConfigureAwait(false);
        }
        public async Task<OperationResult<LoginResult>> AuthenticateAsync(string login, string password, CancellationToken token, string hash = "#compatibility", string mac = "#compatibility")
        {
            const string url = Url + "auth/native/authenticate";

            var parameters = new[]
            {
                KeyValuePair.Create("email", login),
                KeyValuePair.Create("password", password),
                KeyValuePair.Create("service", "syncer"),
                KeyValuePair.Create("machineid", "{\"hash\":\"" + hash + "\",\"mac\":\"" + mac + "\"}")
            };

            var response = await SendGetRequestAsync(url, parameters, token).ConfigureAwait(false);
            if (!response) return response.GetResult();

            var uid = response.Result["userid"]?.Value<string>() ?? throw new NullReferenceException();
            var sid = response.Result["sid"]?.Value<string>() ?? throw new NullReferenceException();
            var result = new LoginResult(login, uid, sid);
            Logger.Log("Successful login: " + result);

            return result;
        }

        OperationResult<JObject> CheckForErrors(HttpResponseMessage response)
        {
            var responsestr = response.Content.ReadAsStringAsync().Result;
            JObject jobject;

            try { jobject = JObject.Parse(responsestr); }
            catch (JsonReaderException) { return OperationResult.Err(@$"HTTP {response.StatusCode}; Message {responsestr}"); }

            var isOk = jobject["isOk"];
            if (isOk is null) return OperationResult.Err(@$"HTTP {response.StatusCode}");
            if (isOk.Value<bool>()) return jobject;

            var errorCode = jobject["errorCode"]?.Value<int>() ?? -1;
            if (errorCode == (int) HttpErrorCode.LoginError) OnLoginError(); // login error

            return OperationResult.Err(@$"HTTP {response.StatusCode}; Error code {errorCode}; Message {jobject["response"]}");
        }

        async Task<OperationResult<JObject>> SendGetRequestAsync(string url, IEnumerable<KeyValuePair<string, string>> values, CancellationToken token)
        {
            for (var i = 0; i < 3; i++)
            {
                try { return CheckForErrors(await JustSendGetRequestAsync(url, values, token).ConfigureAwait(false)); }
                catch { if (i == 2) throw; }
            }

            // should not happen
            throw new InvalidOperationException();
        }
        async Task<HttpResponseMessage> JustSendGetRequestAsync(string url, IEnumerable<KeyValuePair<string, string>> values, CancellationToken token)
        {
            var encodedItems = values.Select(i => WebUtility.UrlEncode(i.Key) + "=" + WebUtility.UrlEncode(i.Value));
            using var content = new StringContent(string.Join("&", encodedItems), null, "application/x-www-form-urlencoded");
            var requeststr = (await content.ReadAsStringAsync().ConfigureAwait(false)).Replace("%40", "@");

            var timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(10_000).Token);
            return await Client.GetAsync(url + "?" + requeststr, HttpCompletionOption.ResponseHeadersRead, timeoutToken.Token).ConfigureAwait(false);
        }

        async Task<OperationResult<JObject>> SendPostRequestAsync(string url, IEnumerable<KeyValuePair<string, string>> values, CancellationToken token)
        {
            for (var i = 0; i < 3; i++)
            {
                try { return CheckForErrors(await JustSendPostRequestAsync(url, values, token).ConfigureAwait(false)); }
                catch { if (i == 2) throw; }
            }

            // should not happen
            throw new InvalidOperationException();
        }
        Task<HttpResponseMessage> JustSendPostRequestAsync(string url, IEnumerable<KeyValuePair<string, string>> values, CancellationToken token)
        {
            var timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(token, new CancellationTokenSource(10_000).Token);
            return Client.PostAsync(url, new FormUrlEncodedContent(values), timeoutToken.Token);
        }


        /// <summary> Write JSON to file, for debugging purposes </summary>
        static void WriteTempFile(string tempath, string urladd, JToken content) => Task.Run(() =>
        {
            if (!Directory.Exists(tempath)) return;

            try
            {
                var filep = Path.Combine(tempath, urladd.Replace("/", "_"));
                var index = Enumerable.Range(0, int.MaxValue).FirstOrDefault(x => !File.Exists(filep + x));

                File.WriteAllTextAsync(filep + index, content.ToString());
            }
            catch { }
        });
    }

    public enum HttpErrorCode
    {
        LoginError = 12,
        MetadataWriteError = -73,
        TooManyDownloads = -33,
    }
    public enum LoginType { Yandex, VKontakte, Facebook, Google }
    public readonly struct LoginResult
    {
        public readonly string Username, UserId, SessionId;

        public LoginResult(string username, string userid, string sid)
        {
            Username = username;
            UserId = userid;
            SessionId = sid;
        }

        public void SaveToConfig()
        {
            Settings.SessionId = SessionId;
            Settings.UserId = UserId;
            Settings.Username = Username;
        }

        public override string ToString() => "Username: " + Username + ", Uid:" + UserId + ", Sid:" + SessionId;
    }
    public readonly struct UserInfo
    {
        public readonly string UserId, Email, Id;

        public UserInfo(string userId, string email, string id)
        {
            UserId = userId;
            Email = email;
            Id = id;
        }
    }
}