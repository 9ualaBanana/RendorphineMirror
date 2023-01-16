using System.Net.Sockets;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common;

public record ApiInstance(HttpClient Client, string SessionId, bool LogRequests = true)
{
    public const string ServerUri = "https://tasks.microstock.plus";
    public const string TaskManagerEndpoint = $"{ServerUri}/rphtaskmgr";

    static readonly Logger Logger = LogManager.GetCurrentClassLogger();


    public ValueTask<OperationResult<T>> ApiGet<T>(string url, string? property, string errorDetails, params (string, string)[] values) =>
        Send<T>(HttpMethod.Get, JustGet, url, property, values, errorDetails);
    public ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, params (string, string)[] values) =>
        Send<T>(HttpMethod.Post, JustPost, url, property, values, errorDetails);
    public ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, HttpContent content) =>
        Send<T, HttpContent>(HttpMethod.Post, JustPost, url, property, content, errorDetails);

    public ValueTask<OperationResult> ApiGet(string url, string errorDetails, params (string, string)[] values) =>
        SendOk(HttpMethod.Get, JustGet, url, values, errorDetails);
    public ValueTask<OperationResult> ApiPost(string url, string errorDetails, params (string, string)[] values) =>
        SendOk(HttpMethod.Post, JustPost, url, values, errorDetails);
    public ValueTask<OperationResult> ApiPost(string url, string errorDetails, HttpContent content) =>
        SendOk(HttpMethod.Post, JustPost, url, content, errorDetails);

    ValueTask<OperationResult> SendOk<TValues>(HttpMethod method, Func<string, TValues, Task<HttpResponseMessage>> func, string url, TValues values, string? errorDetails) =>
        Send<bool, TValues>(method, func, url, "ok", values, errorDetails).Next(v => new OperationResult(v, null));
    ValueTask<OperationResult<T>> Send<T>(HttpMethod method, Func<string, (string, string)[], Task<HttpResponseMessage>> func, string url, string? property, (string, string)[] values, string? errorDetails) =>
        Send<T, (string, string)[]>(method, func, url, property, values, errorDetails);
    ValueTask<OperationResult<T>> Send<T, TValues>(HttpMethod method, Func<string, TValues, Task<HttpResponseMessage>> func, string url, string? property, TValues values, string? errorDetails)
    {
        return Execute(send);

        async ValueTask<OperationResult<T>> send()
        {
            var result = await func(url, values).ConfigureAwait(false);

            var responseJson = await readResponse(result, errorDetails).ConfigureAwait(false);
            if (!responseJson) return responseJson.GetResult();

            return (property is null ? responseJson.Value : responseJson.Value[property])!.ToObject<T>()!;
        }
        async ValueTask<OperationResult<JToken>> readResponse(HttpResponseMessage response, string? errorDetails = null)
        {
            if (!response.IsSuccessStatusCode)
                return await asOpResult(null);

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new JsonTextReader(new StreamReader(stream));
            return await asOpResult(JToken.Load(reader));


            async ValueTask<OperationResult<JToken>> asOpResult(JToken? responseJson)
            {
                var logmsg = $"{(errorDetails is null ? $"{errorDetails} " : string.Empty)}[{method.Method} {url} ";

                logmsg += values switch
                {
                    (string, string)[] pcontent => string.Join('&', pcontent.Select(x => x.Item1 + "=" + x.Item2)),
                    FormUrlEncodedContent fcontent => await fcontent.ReadAsStringAsync(),
                    _ => null,
                };
                logmsg += "]";

                var retmsg = errorDetails ?? string.Empty;

                var ok = responseJson?["ok"]?.Value<int>() == 1;
                var errcode = responseJson?["errorcode"]?.Value<int>();
                var errmsg = responseJson?["errormessage"]?.Value<string>();


                if (!response.IsSuccessStatusCode || responseJson is null)
                {
                    logmsg += $": HTTP {response.StatusCode}";
                    retmsg += $": HTTP {response.StatusCode}";
                }
                else if (!ok)
                {
                    logmsg += $": {responseJson.ToString(Formatting.None)}";
                    retmsg += $": error {errcode}: {errmsg ?? "<no message>"}";
                }

                if (LogRequests) Logger.Trace(logmsg);


                if (response.IsSuccessStatusCode && ok)
                    return new OperationResult<JToken>(OperationResult.Succ() with { HttpData = new(response, null) }, responseJson);

                return OperationResult.Err(retmsg) with { HttpData = new(response, errcode) };
            }
        }
    }

    public HttpContent ToContent((string, string)[] values) => new FormUrlEncodedContent(values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)));
    public async Task<HttpResponseMessage> JustPost(string url, (string, string)[] values)
    {
        using var content = ToContent(values);
        return await Client.JustPost(url, content).ConfigureAwait(false);
    }
    public Task<HttpResponseMessage> JustGet(string url, (string, string)[] values)
    {
        var str = string.Join('&', values.Select(x => x.Item1 + "=" + HttpUtility.UrlEncode(x.Item2)));
        if (str.Length != 0) str = "?" + str;

        return Client.JustGet(url + str);
    }
    public Task<HttpResponseMessage> JustPost(string url, HttpContent content) => Client.PostAsync(url, content);
    public Task<HttpResponseMessage> JustGet(string url) => Client.GetAsync(url);

    public Task<Stream> Download(string url) => Client.GetStreamAsync(url);
    public Task<HttpResponseMessage> Get(string url) => Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);


    async ValueTask<OperationResult<T>> Execute<T>(Func<ValueTask<OperationResult<T>>> func)
    {
        while (true)
        {
            try
            {
                var result = await func().ConfigureAwait(false);
                if (result.EString.HttpData is { } httperr && !httperr.IsSuccessStatusCode)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    continue;
                }

                return result;
            }
            catch (SocketException)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                continue;
            }
            catch (Exception ex) { return OperationResult.Err(ex); }
        }
    }

    // Create a wrapper out of it instead.
    //static async Task<HttpResponseMessage> TrySendRequestAsync(
    //    Func<Task<HttpResponseMessage>> requestCallback)
    //{
    //    var attempts = 0;
    //    while (true)
    //    {
    //        try
    //        {
    //            var response = await requestCallback();
    //            await GetJsonFromResponseIfSuccessful(response);
    //            return response;
    //        }
    //        catch (HttpRequestException ex)
    //        {
    //            if (++attempts >= requestOptions.RetryAttempts)
    //                throw new HttpRequestException($"Request couldn't succeed after {attempts} attempts.", ex);

    //            await Task.Delay(requestOptions.RetryInterval, requestOptions.CancellationToken);
    //        }
    //    }
    //}

    public static async ValueTask<JToken> GetJsonFromResponseIfSuccessfulAsync(HttpResponseMessage response, string? errorDetails = null)
    {
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new JsonTextReader(new StreamReader(stream));
        var responseJson = JToken.Load(reader);
        var responseStatusCode = responseJson["ok"]?.Value<int>();
        if (responseStatusCode != 1)
        {
            if (responseJson["errormessage"]?.Value<string>() is { } errmsg)
                throw new HttpRequestException(errmsg);

            if (responseJson["errorcode"]?.Value<string>() is { } errcode)
                throw new HttpRequestException($"{errorDetails} Server responded with {errcode} error code");

            throw new HttpRequestException($"{errorDetails} Server responded with {responseStatusCode} status code");
        }

        return responseJson;
    }
}