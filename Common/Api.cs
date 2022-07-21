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


        public static ValueTask<OperationResult<T>> ApiGet<T>(string url, string? property, string? errorDetails = null, params (string, string)[] values) =>
            Send<T>(JustGet, url, property, values, errorDetails);
        public static ValueTask<OperationResult<T>> ApiPost<T>(string url, string? property, string? errorDetails = null, params (string, string)[] values) =>
            Send<T>(JustPost, url, property, values, errorDetails);

        public static ValueTask<OperationResult> ApiGet(string url, string? errorDetails = null, params (string, string)[] values) =>
            Send(JustGet, url, values, errorDetails);
        public static ValueTask<OperationResult> ApiPost(string url, string? errorDetails = null, params (string, string)[] values) =>
            Send(JustPost, url, values, errorDetails);

        static ValueTask<OperationResult> Send(Func<string, (string, string)[], Task<HttpResponseMessage>> func, string url, (string, string)[] values, string? errorDetails) =>
            Send<bool>(func, url, "ok", values, errorDetails).Next(v => new OperationResult(v, null));
        static ValueTask<OperationResult<T>> Send<T>(Func<string, (string, string)[], Task<HttpResponseMessage>> func, string url, string? property, (string, string)[] values, string? errorDetails)
        {
            return Execute(send);

            async ValueTask<T> send()
            {
                var result = await func(url, values).ConfigureAwait(false);

                var responseJson = await GetJsonFromResponseIfSuccessful(result, errorDetails).ConfigureAwait(false);
                return (property is null ? responseJson : responseJson[property])!.ToObject<T>()!;
            }
        }

        public static async Task<HttpResponseMessage> JustPost(string url, (string, string)[] values)
        {
            using var content = new FormUrlEncodedContent(values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)));
            return await JustPost(url, content).ConfigureAwait(false);
        }
        public static Task<HttpResponseMessage> JustGet(string url, (string, string)[] values)
        {
            var str = string.Join('&', values.Select(x => x.Item1 + "=" + HttpUtility.UrlEncode(x.Item2)));
            if (str.Length != 0) str = "?" + str;

            return JustGet(url + str);
        }
        public static Task<HttpResponseMessage> JustPost(string url, HttpContent content) => Client.PostAsync(url, content);
        public static Task<HttpResponseMessage> JustGet(string url) => Client.GetAsync(url);

        public static Task<Stream> Download(string url) => Client.GetStreamAsync(url);


        static async ValueTask<OperationResult<T>> Execute<T>(Func<ValueTask<T>> func)
        {
            while (true)
            {
                try { return await func().ConfigureAwait(false); }
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

        static async ValueTask<JToken> GetJsonFromResponseIfSuccessful(HttpResponseMessage response, string? errorDetails = null)
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