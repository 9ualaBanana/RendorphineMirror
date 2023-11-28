using System.Net.Http.Headers;
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
    public async ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, HttpContent? content) =>
        await Send<T>(property, errorDetails, async () => await Client.PostAsync(url, content, CancellationToken)).ConfigureAwait(false);

    public async ValueTask<OperationResult> ApiGet(string url, string errorDetails, params (string, string)[] values) =>
        await ApiGet<bool>(url, "ok", errorDetails, values).Next(r => OperationResult.Succ());
    public async ValueTask<OperationResult> ApiPost(string url, string errorDetails, params (string, string)[] values) =>
        await ApiPost<bool>(url, "ok", errorDetails, values).Next(r => OperationResult.Succ());
    public async ValueTask<OperationResult> ApiPost(string url, string errorDetails, HttpContent? content) =>
        await ApiPost<bool>(url, "ok", errorDetails, content).Next(r => OperationResult.Succ());

    public async ValueTask<OperationResult<T>> ApiSend<T>(HttpRequestMessage request, string? property, string errorDetails) =>
        await Send<T>(property, errorDetails, async () => await Client.SendAsync(request, CancellationToken));


    async Task<OperationResult<T>> Send<T>(string? property, string errorDetails, Func<Task<HttpResponseMessage>> func)
    {
        return await RetryUntilSuccess(async () =>
            await SendRead(
                func,
                response =>
                    from json in ReadResponseJson(response, errorDetails)
                    select (property is null ? json : json[property])!.ToObject<T>()!
            ).ConfigureAwait(false)
        ).ConfigureAwait(false);
    }


    /// <inheritdoc cref="RetryUntilSuccess{T}(Func{Task{OperationResult{T}}})"/>
    protected async Task<OperationResult> RetryUntilSuccess(Func<Task<OperationResult>> func) =>
        await RetryUntilSuccess<Empty>(async () => await func())
            .Next(_ => OperationResult.Succ());

    /// <summary>
    /// Repeatedly invokes <paramref name="func"/> until it succeeds, checking the success using <see cref="NeedsToRetryRequest(OperationResult)"/>.
    /// Does not retry upon receiving an exception, except <see cref="SocketException"/>.
    /// </summary>
    protected async Task<OperationResult<T>> RetryUntilSuccess<T>(Func<Task<OperationResult<T>>> func)
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
            catch (Exception ex) when (ex is SocketException or HttpRequestException { InnerException: SocketException })
            {
                await Task.Delay(RequestRetryDelay).ConfigureAwait(false);
                continue;
            }
            catch (Exception ex) { return OperationResult.Err(ex); }
        }
    }


    protected static async Task<OperationResult> SendRead(Func<Task<HttpResponseMessage>> sendfunc, Func<HttpResponseMessage, Task<OperationResult>> readfunc) =>
        await SendRead<Empty>(sendfunc, async response => await readfunc(response))
            .Next(_ => OperationResult.Succ());
    protected static async Task<OperationResult<T>> SendRead<T>(Func<Task<HttpResponseMessage>> sendfunc, Func<HttpResponseMessage, Task<OperationResult<T>>> readfunc)
    {
        using var result = await sendfunc().ConfigureAwait(false);
        return await readfunc(result);
    }

    async Task<OperationResult<JToken>> ReadResponseJson(HttpResponseMessage response, string errorDetails)
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
    public abstract HttpContent ToPostContent((string, string)[] values);
    public static HttpContent ToPostContent(JObject json)
    {
        var data = JsonConvert.SerializeObject(json, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        return new StringContent(data, new MediaTypeHeaderValue("application/json"));
    }

    public abstract Task<OperationResult> ResponseToResult(HttpResponseMessage response, JToken? responseJson, string errorDetails, CancellationToken token);
    public async Task<OperationResult<JToken>> ResponseJsonToOpResult(HttpResponseMessage response, JToken? responseJson, string errorDetails, CancellationToken token) =>
        await ResponseToResult(response, responseJson, errorDetails, token)
            .Next(() => responseJson.ThrowIfNull().AsOpResult());

    public static string ToQuery(params (string, string)[] values) => string.Join('&', values.Select(x => x.Item1 + "=" + HttpUtility.UrlEncode(x.Item2)));
    public static string AppendQuery(string url, params (string, string)[] values)
    {
        if (values.Length == 0)
            return url;

        if (url.Contains('?'))
            return url + $"&{ToQuery(values)}";
        return url + $"?{ToQuery(values)}";
    }

    protected async Task LogRequest(HttpResponseMessage response, JToken? responseJson, string errorDetails)
    {
        if (!LogRequests) return;
        await LogRequest(response, responseJson, errorDetails, Logger, CancellationToken);
    }
    public static async Task LogRequest(HttpResponseMessage response, JToken? responseJson, string errorDetails, ILogger logger, CancellationToken token)
    {
        var text = $"[{errorDetails}] [{response.RequestMessage?.Method.Method} {response.RequestMessage?.RequestUri}";
        if (response.RequestMessage?.Method == HttpMethod.Post)
            text += $" Content{{ {await ContentToString(response.RequestMessage.Content, token)} }}";
        if (response.RequestMessage?.Headers.Any() == true)
            text += $" Headers{{ {HeadersToString(response.RequestMessage.Headers)} }}";
        text += $"]: HTTP {(int) response.StatusCode}";
        if (responseJson is not null)
            text += $": {responseJson?.ToString(Formatting.None) ?? "<no message>"}";

        logger.LogTrace(text);
    }

    protected async Task<string> ContentToString(object? content) => await ContentToString(content, CancellationToken);
    public static async Task<string> ContentToString(object? content, CancellationToken token)
    {
        try
        {
            return content switch
            {
                (string, string)[] pcontent => string.Join('&', pcontent.Select(x => x.Item1 + "=" + HttpUtility.UrlDecode(x.Item2))),
                FormUrlEncodedContent c => HttpUtility.UrlDecode(await c.ReadAsStringAsync(token)),
                StringContent c => await c.ReadAsStringAsync(token),
                MultipartContent c => $"Multipart [{string.Join(", ", await Task.WhenAll(c.Select(async c => $"{{ {c.Headers.ContentDisposition?.Name ?? "<noname>"} : {await ContentToString(c, token)} }}")))}]",
                { } => content.GetType().Name,
                _ => "<nocontent>",
            };
        }
        catch (Exception ex)
        {
            return $"<{ex.GetType().Name} at {content?.GetType()}>";
        }
    }
    public static string HeadersToString(HttpRequestHeaders headers)
    {
        return string.Join("; ", headers.Select(header => $"{header.Key}: {string.Join(", ", header.Value)}"));
    }

    public async Task<OperationResult> ApiGetFile(string url, string filename, string errorDetails, params (string, string)[] values) =>
        await SendFile(filename, errorDetails, async () => await Client.GetAsync(AppendQuery(url, values), CancellationToken));
    public async Task<OperationResult> ApiPostFile(string url, string filename, string errorDetails, params (string, string)[] values)
    {
        using var content = ToPostContent(values);
        return await SendFile(filename, errorDetails, async () => await Client.PostAsync(url, content, CancellationToken));
    }
    public async Task<OperationResult> ApiPostFile(string url, string filename, string errorDetails, HttpContent content) =>
        await SendFile(filename, errorDetails, async () => await Client.PostAsync(url, content, CancellationToken)).ConfigureAwait(false);
    public async Task<OperationResult> ApiPostFile(string url, string filename, string errorDetails, JObject json)
    {
        using var content = ToPostContent(json);
        return await SendFile(filename, errorDetails, async () => await Client.PostAsync(url, content, CancellationToken));
    }
    public async Task<OperationResult> ApiSendFile(HttpRequestMessage request, string filename, string errorDetails) =>
        await SendFile(filename, errorDetails, async () => await Client.SendAsync(request, CancellationToken));

    async Task<OperationResult> SendFile(string filename, string errorDetails, Func<Task<HttpResponseMessage>> func)
    {
        return await RetryUntilSuccess(async () =>
            await SendRead(
                func,
                response => ReadResponseFile(response, filename, errorDetails)
            ).ConfigureAwait(false)
        ).ConfigureAwait(false);
    }
    async Task<OperationResult> ReadResponseFile(HttpResponseMessage response, string filename, string errorDetails)
    {
        using var stream = await response.Content.ReadAsStreamAsync(CancellationToken).ConfigureAwait(false);

        try
        {
            if (!response.IsSuccessStatusCode)
            {
                using var reader = new JsonTextReader(new StreamReader(stream));
                return await ResponseToResult(response, await JToken.LoadAsync(reader), errorDetails, CancellationToken)
                    .Next(writefile);
            }

            return await ResponseToResult(response, null, errorDetails, CancellationToken)
                .Next(writefile);
        }
        catch
        {
            return await ResponseToResult(response, null, errorDetails, CancellationToken)
                .Next(writefile);
        }


        async Task<OperationResult> writefile()
        {
            using var file = File.Open(filename, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(file);

            return OperationResult.Succ();
        }
    }


    public static implicit operator HttpClient(ApiBase api) => api.Client;
}
