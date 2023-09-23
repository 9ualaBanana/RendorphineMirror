using System.Net.Sockets;
using System.Web;

namespace Common;

public abstract record ApiBase
{
    public bool LogRequests { get; init; } = true;
    public CancellationToken CancellationToken { get; init; } = default;
    public TimeSpan RequestRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    public HttpClient Client { get; init; }
    public ILogger<ApiBase> Logger { get; }

    protected ApiBase(HttpClient client, ILogger<ApiBase> logger)
    {
        Client = client;
        Logger = logger;
    }

    public async ValueTask<OperationResult<T>> ApiGet<T>(string url, string? property, string errorDetails, params (string, string)[] values) =>
        await Send<T>(property, errorDetails, async () => await Client.GetAsync(AppendQuery(url, values), CancellationToken));
    public async ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, params (string, string)[] values)
    {
        using var content = ToPostContent(values);
        return await Send<T>(property, errorDetails, async () => await Client.PostAsync(url, content, CancellationToken));
    }
    public async ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, HttpContent content) =>
        await Send<T>(property, errorDetails, async () => await Client.PostAsync(url, content, CancellationToken)).ConfigureAwait(false);

    public async ValueTask<OperationResult> ApiGet(string url, string errorDetails, params (string, string)[] values) =>
        await ApiGet<bool>(url, "ok", errorDetails, values).Next(r => OperationResult.Succ());
    public async ValueTask<OperationResult> ApiPost(string url, string errorDetails, params (string, string)[] values) =>
        await ApiPost<bool>(url, "ok", errorDetails, values).Next(r => OperationResult.Succ());
    public async ValueTask<OperationResult> ApiPost(string url, string errorDetails, HttpContent content) =>
        await ApiPost<bool>(url, "ok", errorDetails, content).Next(r => OperationResult.Succ());


    async Task<OperationResult<T>> Send<T>(string? property, string errorDetails, Func<Task<HttpResponseMessage>> func) =>
        await RetryUntilSuccess(async () => await SendRead<T>(property, errorDetails, func).ConfigureAwait(false)).ConfigureAwait(false);

    /// <summary> 
    /// Repeatedly invokes <paramref name="func"/> until it succeeds, checking the success using <see cref="NeedsToRetryRequest(OperationResult)"/>.
    /// Does not retry upon receiving an exception, except <see cref="SocketException"/>.
    /// </summary>
    async Task<OperationResult<T>> RetryUntilSuccess<T>(Func<Task<OperationResult<T>>> func)
    {
        while (true)
        {
            try
            {
                var result = await func().ConfigureAwait(false);

                if (NeedsToRetryRequest(result.GetResult()))
                {
                    await Task.Delay(RequestRetryDelay).ConfigureAwait(false);
                    continue;
                }

                return result;
            }
            catch (SocketException)
            {
                await Task.Delay(RequestRetryDelay).ConfigureAwait(false);
                continue;
            }
            catch (Exception ex) { return OperationResult.Err(ex); }
        }
    }
    async Task<OperationResult<T>> SendRead<T>(string? property, string errorDetails, Func<Task<HttpResponseMessage>> func)
    {
        using var result = await func().ConfigureAwait(false);

        var responseJson = await ReadResponse(result, errorDetails).ConfigureAwait(false);
        if (!responseJson) return responseJson.GetResult();

        return (property is null ? responseJson.Value : responseJson.Value[property])!.ToObject<T>()!;
    }
    async Task<OperationResult<JToken>> ReadResponse(HttpResponseMessage response, string errorDetails)
    {
        using var stream = await response.Content.ReadAsStreamAsync(CancellationToken).ConfigureAwait(false);

        try
        {
            if (stream.Length == 0)
                return await ResponseJsonToOpResult(response, null, errorDetails, CancellationToken);

            using var reader = new JsonTextReader(new StreamReader(stream));
            return await ResponseJsonToOpResult(response, await JToken.LoadAsync(reader), errorDetails, CancellationToken);
        }
        catch
        {
            return await ResponseJsonToOpResult(response, null, errorDetails, CancellationToken);
        }
    }


    protected abstract bool NeedsToRetryRequest(OperationResult result);
    public abstract Task<OperationResult<JToken>> ResponseJsonToOpResult(HttpResponseMessage response, JToken? responseJson, string errorDetails, CancellationToken token);
    public abstract HttpContent ToPostContent((string, string)[] values);

    public static string ToQuery((string, string)[] values) => string.Join('&', values.Select(x => x.Item1 + "=" + HttpUtility.UrlEncode(x.Item2)));
    public static string AppendQuery(string url, (string, string)[] values)
    {
        if (values.Length == 0)
            return url;

        if (url.Contains('?'))
            return url + $"&{ToQuery(values)}";
        return url + $"?{ToQuery(values)}";
    }


    public static implicit operator HttpClient(ApiBase api) => api.Client;
}
