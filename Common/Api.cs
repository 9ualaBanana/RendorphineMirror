using System.Net.Sockets;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class Api
    {
        public const string ServerUri = "https://tasks.microstock.plus";
        public const string TaskManagerEndpoint = $"{ServerUri}/rphtaskmgr";

        public static readonly HttpClient Client = new();


        public static ValueTask<OperationResult<T>> ApiGet<T>(this HttpClient client, string url, string? property, string errorDetails, params (string, string)[] values) =>
            Send<T>(client.JustGet, url, property, values, errorDetails);
        public static ValueTask<OperationResult<T>> ApiPost<T>(this HttpClient client, string url, string? property, string errorDetails, params (string, string)[] values) =>
            Send<T>(client.JustPost, url, property, values, errorDetails);
        public static ValueTask<OperationResult<T>> ApiPost<T>(this HttpClient client, string url, string? property, string errorDetails, HttpContent content) =>
            Send<T, HttpContent>(client.JustPost, url, property, content, errorDetails);

        public static ValueTask<OperationResult> ApiGet(this HttpClient client, string url, string errorDetails, params (string, string)[] values) =>
            SendOk(client.JustGet, url, values, errorDetails);
        public static ValueTask<OperationResult> ApiPost(this HttpClient client, string url, string errorDetails, params (string, string)[] values) =>
            SendOk(client.JustPost, url, values, errorDetails);
        public static ValueTask<OperationResult> ApiPost(this HttpClient client, string url, string errorDetails, HttpContent content) =>
            SendOk(client.JustPost, url, content, errorDetails);


        public static ValueTask<OperationResult<T>> ApiGet<T>(string url, string? property, string errorDetails, params (string, string)[] values) =>
            Client.ApiGet<T>(url, property, errorDetails, values);
        public static ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, params (string, string)[] values) =>
            Client.ApiPost<T>(url, property, errorDetails, values);
        public static ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string errorDetails, HttpContent content) =>
            Client.ApiPost<T>(url, property, errorDetails, content);

        public static ValueTask<OperationResult> ApiGet(string url, string errorDetails, params (string, string)[] values) =>
            Client.ApiGet(url, errorDetails, values);
        public static ValueTask<OperationResult> ApiPost(string url, string errorDetails, params (string, string)[] values) =>
            Client.ApiPost(url, errorDetails, values);
        public static ValueTask<OperationResult> ApiPost(string url, string errorDetails, HttpContent content) =>
            Client.ApiPost(url, errorDetails, content);


        static ValueTask<OperationResult> SendOk<TValues>(Func<string, TValues, Task<HttpResponseMessage>> func, string url, TValues values, string? errorDetails) =>
            Send<bool, TValues>(func, url, "ok", values, errorDetails).Next(v => new OperationResult(v, null));
        static ValueTask<OperationResult<T>> Send<T>(Func<string, (string, string)[], Task<HttpResponseMessage>> func, string url, string? property, (string, string)[] values, string? errorDetails) =>
            Send<T, (string, string)[]>(func, url, property, values, errorDetails);
        static ValueTask<OperationResult<T>> Send<T, TValues>(Func<string, TValues, Task<HttpResponseMessage>> func, string url, string? property, TValues values, string? errorDetails)
        {
            return Execute(send);

            async ValueTask<OperationResult<T>> send()
            {
                var result = await func(url, values).ConfigureAwait(false);

                var responseJson = await readResponse(result, errorDetails).ConfigureAwait(false);
                if (!responseJson) return responseJson.GetResult();

                return (property is null ? responseJson.Value : responseJson.Value[property])!.ToObject<T>()!;
            }
            static async ValueTask<OperationResult<JToken>> readResponse(HttpResponseMessage response, string? errorDetails = null)
            {
                if (!response.IsSuccessStatusCode)
                    return OperationResult.Err($"{errorDetails}: Server responded with HTTP {response.StatusCode}") with { HttpData = new(response.StatusCode, null) };

                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var reader = new JsonTextReader(new StreamReader(stream));
                var responseJson = JToken.Load(reader);
                var responseStatusCode = responseJson["ok"]?.Value<int>();
                if (responseStatusCode != 1)
                {
                    var errmsg = responseJson["errormessage"]?.Value<string>();
                    var errcode = responseJson["errorcode"]?.Value<int>();

                    errmsg = $"{errorDetails}: Server responded with HTTP {response.StatusCode} & {errcode} error code: {errmsg ?? "<no message>"}";
                    return OperationResult.Err(errmsg) with { HttpData = new(response.StatusCode, errcode) };
                }

                return responseJson;
            }
        }

        public static HttpContent ToContent((string, string)[] values) => new FormUrlEncodedContent(values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)));
        public static async Task<HttpResponseMessage> JustPost(this HttpClient client, string url, (string, string)[] values)
        {
            using var content = ToContent(values);
            return await client.JustPost(url, content).ConfigureAwait(false);
        }
        public static Task<HttpResponseMessage> JustGet(this HttpClient client, string url, (string, string)[] values)
        {
            var str = string.Join('&', values.Select(x => x.Item1 + "=" + HttpUtility.UrlEncode(x.Item2)));
            if (str.Length != 0) str = "?" + str;

            return client.JustGet(url + str);
        }
        public static Task<HttpResponseMessage> JustPost(this HttpClient client, string url, HttpContent content) => client.PostAsync(url, content);
        public static Task<HttpResponseMessage> JustGet(this HttpClient client, string url) => client.GetAsync(url);

        public static Task<Stream> Download(string url) => Client.Download(url);
        public static Task<Stream> Download(this HttpClient client, string url) => client.GetStreamAsync(url);

        public static Task<HttpResponseMessage> Get(string url) => Client.Get(url);
        public static Task<HttpResponseMessage> Get(this HttpClient client, string url) => client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);


        static async ValueTask<OperationResult<T>> Execute<T>(Func<ValueTask<OperationResult<T>>> func)
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
}