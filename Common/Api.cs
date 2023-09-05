using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Common;

public record Api(HttpClient Client)
{
    public bool LogRequests { get; init; } = true;
    public CancellationToken CancellationToken { get; init; } = default;

    public const string ServerUri = "https://tasks.microstock.plus";
    public const string TaskManagerEndpoint = $"{ServerUri}/rphtaskmgr";
    public static readonly Uri TaskLauncherEndpoint = new($"{ServerUri}/rphtasklauncher/");
    public const string ContentDBEndpoint = $"https://cdb.microstock.plus/contentdb";

    public static readonly HttpClient GlobalClient = new();
    public static readonly Api Default = new(GlobalClient);
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static (string, string)[] AddSessionId(string sessionid, params (string, string)[] values)
    {
        if (values.Any(x => x.Item1 == "sessionid")) return values;
        return values.Append(("sessionid", sessionid)).ToArray();
    }
    public static (string, string)[] SignRequest(string key, params (string, string)[] values) => values.Append(("sign", CalculateSign(key, values))).ToArray();
    public static string CalculateSign(string key, params (string, string)[] values)
    {
        var content = string.Join('|', values.Select(x => x.Item2));
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
    }

    public ValueTask<OperationResult<T>> ApiGet<T>(string url, string? property, string errorDetails, params (string, string)[] values) =>
        Send<T>(JustGet, url, property, values, errorDetails);
    public ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, params (string, string)[] values) =>
        Send<T>(JustPost, url, property, values, errorDetails);
    public ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, HttpContent content) =>
        Send<T, HttpContent>(JustPost, url, property, content, errorDetails);

    public ValueTask<OperationResult> ApiGet(string url, string errorDetails, params (string, string)[] values) =>
        SendOk(JustGet, url, values, errorDetails);
    public ValueTask<OperationResult> ApiPost(string url, string errorDetails, params (string, string)[] values) =>
        SendOk(JustPost, url, values, errorDetails);
    public ValueTask<OperationResult> ApiPost(string url, string errorDetails, HttpContent content) =>
        SendOk(JustPost, url, content, errorDetails);

    ValueTask<OperationResult> SendOk<TValues>(Func<string, TValues, Task<HttpResponseMessage>> func, string url, TValues values, string? errorDetails) =>
        Send<bool, TValues>(func, url, "ok", values, errorDetails).Next(v => new OperationResult(v, null));
    ValueTask<OperationResult<T>> Send<T>(Func<string, (string, string)[], Task<HttpResponseMessage>> func, string url, string? property, (string, string)[] values, string? errorDetails) =>
        Send<T, (string, string)[]>(func, url, property, values, errorDetails);
    ValueTask<OperationResult<T>> Send<T, TValues>(Func<string, TValues, Task<HttpResponseMessage>> func, string url, string? property, TValues values, string? errorDetails)
    {
        return execute();

        async ValueTask<OperationResult<T>> execute()
        {
            while (true)
            {
                try
                {
                    var result = await send().ConfigureAwait(false);

                    // true only when http code is non-success (except 400) and there's no response json
                    var httperr = result.Error is HttpError
                    {
                        IsSuccessStatusCode: false,
                        //StatusCode: not System.Net.HttpStatusCode.BadRequest,
                        ErrorCode: null,
                    };

                    if (httperr)
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
        async ValueTask<OperationResult<T>> send()
        {
            var result = await func(url, values).ConfigureAwait(false);

            var responseJson = await readResponse(result, errorDetails).ConfigureAwait(false);
            if (!responseJson) return responseJson.GetResult();

            return (property is null ? responseJson.Value : responseJson.Value[property])!.ToObject<T>()!;
        }
        async ValueTask<OperationResult<JToken>> readResponse(HttpResponseMessage response, string? errorDetails = null)
        {
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(CancellationToken).ConfigureAwait(false);
                if (stream.Length == 0)
                    return await ResponseJsonToOpResult(response, values, null, errorDetails, LogRequests, CancellationToken);

                using var reader = new JsonTextReader(new StreamReader(stream));
                return await ResponseJsonToOpResult(response, values, await JToken.LoadAsync(reader), errorDetails, LogRequests, CancellationToken);
            }
            catch
            {
                return await ResponseJsonToOpResult(response, values, null, errorDetails, LogRequests, CancellationToken);
            }
        }
    }

    public static async ValueTask<OperationResult<JToken>> ResponseJsonToOpResult(HttpResponseMessage response, object? content, JToken? responseJson, string? errorDetails, bool log, CancellationToken token)
    {
        var logmsg = $"{(errorDetails is null ? $"{errorDetails} " : string.Empty)}[{response.RequestMessage?.Method.Method} {response.RequestMessage?.RequestUri} ";

        logmsg += "{";
        async ValueTask<string> contentToString(object? content) =>
            content switch
            {
                (string, string)[] pcontent => string.Join('&', pcontent.Select(x => x.Item1 + "=" + HttpUtility.UrlDecode(x.Item2))),
                FormUrlEncodedContent c => HttpUtility.UrlDecode(await c.ReadAsStringAsync(token)),
                StringContent c => await c.ReadAsStringAsync(token),
                MultipartContent c => $"Multipart [{string.Join(", ", await Task.WhenAll(c.Select(async c => $"{{ {c.Headers.ContentDisposition?.Name ?? "<noname>"} : {await contentToString(c)} }}")))}]",
                { } => content.GetType().Name,
                _ => "no content",
            };

        logmsg += await contentToString(content);
        logmsg += "}]";

        var retmsg = errorDetails ?? string.Empty;

        var errcode = responseJson?["errorcode"]?.Value<int>();
        var errmsg = responseJson?["errormessage"]?.Value<string>();
        var ok =
            response.IsSuccessStatusCode
            && responseJson?["ok"]?.Value<int>() is null or 1
            && errcode is null or 0
            && errmsg is null;


        if (!response.IsSuccessStatusCode || responseJson is null)
        {
            logmsg += $": HTTP {response.StatusCode}";
            retmsg += $": HTTP {response.StatusCode}";
        }
        if (!ok && responseJson is not null)
        {
            logmsg += $": {responseJson.ToString(Formatting.None)}";
            retmsg += $": error {errcode}: {errmsg ?? "<no message>"}";
        }

        if (log) Logger.Trace(logmsg);


        if (response.IsSuccessStatusCode && ok)
            return new OperationResult<JToken>(OperationResult.Succ(), responseJson);

        return OperationResult.Err(new HttpError(errmsg, response, errcode));
    }

    public static HttpContent ToContent((string, string)[] values) => new FormUrlEncodedContent(values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)));
    public static string ToGetContent((string, string)[] values) => string.Join('&', values.Select(x => x.Item1 + "=" + HttpUtility.UrlEncode(x.Item2)));

    public async Task<HttpResponseMessage> JustPost(string url, (string, string)[] values)
    {
        using var content = ToContent(values);
        return await JustPost(url, content).ConfigureAwait(false);
    }
    public Task<HttpResponseMessage> JustGet(string url, (string, string)[] values)
    {
        var str = ToGetContent(values);
        if (str.Length != 0) str = "?" + str;

        return JustGet(url + str);
    }
    public Task<HttpResponseMessage> JustPost(string url, HttpContent? content) => Client.PostAsync(url, content, CancellationToken);
    public Task<HttpResponseMessage> JustGet(string url) => Client.GetAsync(url, CancellationToken);

    public Task<Stream> Download(string url) => Client.GetStreamAsync(url, CancellationToken);
    public Task<HttpResponseMessage> Get(string url) => Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, CancellationToken);


    public static implicit operator HttpClient(Api api) => api.Client;
}