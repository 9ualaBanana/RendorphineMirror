using System.Collections.Immutable;
using System.Net.Sockets;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class Api
    {
        public const string ServerUri = "https://tasks.microstock.plus";
        public const string AccountsEndpoint = $"{ServerUri}/rphaccounts";
        public const string TaskManagerEndpoint = $"{ServerUri}/rphtaskmgr";

        static readonly HttpClient Client = new();

        public static ValueTask<OperationResult<T>> ApiPost<T>(string url, string property, string? errorDetails = null, params (string, string)[] values) =>
            ApiPost<T>(url, property, values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)), errorDetails);
        public static ValueTask<OperationResult<T>> ApiPost<T>(string url, string property, IEnumerable<KeyValuePair<string, string>> values, string? errorDetails = null) =>
            Execute(() => Post<T>(url, property, values, errorDetails));

        public static ValueTask<OperationResult<T>> ApiGet<T>(string url, string property, string? errorDetails = null, params (string, string)[] values) =>
            ApiGet<T>(url, property, values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)), errorDetails);
        public static ValueTask<OperationResult<T>> ApiGet<T>(string url, string property, IEnumerable<KeyValuePair<string, string>> values, string? errorDetails = null) =>
            Execute(() => Get<T>(url, property, values, errorDetails));

        static async ValueTask<T> Post<T>(string url, string property, IEnumerable<KeyValuePair<string, string>> values, string? errorDetails = null)
        {
            var responseJson = await Post(url, values, errorDetails).ConfigureAwait(false);
            return responseJson[property]!.ToObject<T>()!;
        }
        static async ValueTask<T> Get<T>(string url, string property, IEnumerable<KeyValuePair<string, string>> values, string? errorDetails = null)
        {
            var responseJson = await Get(url, values, errorDetails).ConfigureAwait(false);
            return responseJson[property]!.ToObject<T>()!;
        }

        public static ValueTask<OperationResult> ApiPost(string url, string? errorDetails = null, params (string, string)[] values) =>
            ApiPost(url, values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)), errorDetails);
        public static ValueTask<OperationResult> ApiPost(string url, IEnumerable<KeyValuePair<string, string>> values, string? errorDetails = null) =>
            Execute(() => Post(url, values, errorDetails)).Next(_ => true);
        public static ValueTask<OperationResult> ApiGet(string url, string? errorDetails = null, params (string, string)[] values) =>
            ApiGet(url, values.Select(x => KeyValuePair.Create(x.Item1, x.Item2)), errorDetails);
        public static ValueTask<OperationResult> ApiGet(string url, IEnumerable<KeyValuePair<string, string>> values, string? errorDetails = null) =>
            Execute(() => Get(url, values, errorDetails)).Next(_ => true);

        static async Task<JToken> Post(string url, IEnumerable<KeyValuePair<string, string>> values, string? errorDetails = null)
        {
            using var content = new FormUrlEncodedContent(values);
            var result = await Client.PostAsync(url, content).ConfigureAwait(false);
            return await GetJsonFromResponseIfSuccessful(result, errorDetails).ConfigureAwait(false);
        }
        static async Task<JToken> Get(string url, IEnumerable<KeyValuePair<string, string>> values, string? errorDetails = null)
        {
            var str = string.Join('&', values.Select(x => x.Key + "=" + HttpUtility.UrlEncode(x.Value)));
            if (str.Length != 0) str = "?" + str;

            var result = await Client.GetAsync(url + str).ConfigureAwait(false);
            return await GetJsonFromResponseIfSuccessful(result, errorDetails).ConfigureAwait(false);
        }


        static ValueTask<OperationResult<T>> Execute<T>(Func<ValueTask<T>> func) => Execute(() => func().AsTask());
        static async ValueTask<OperationResult<T>> Execute<T>(Func<Task<T>> func)
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

        public static async Task<HttpResponseMessage> TrySendRequestAsync(
            Func<Task<HttpResponseMessage>> requestCallback,
            RequestOptions requestOptions)
        {
            var attempts = 0;
            while (true)
            {
                try
                {
                    // Port to Newtonsoft.Json
                    var response = await requestCallback();
                    await GetJsonFromResponseIfSuccessful(response);
                    return response;
                }
                catch (HttpRequestException ex)
                {
                    if (++attempts >= requestOptions.RetryAttempts)
                        throw new HttpRequestException($"Request couldn't succeed after {attempts} attempts.", ex);

                    await Task.Delay(requestOptions.RetryInterval, requestOptions.CancellationToken);
                }
            }
        }

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


        public static ValueTask<OperationResult<ImmutableDictionary<string, SoftwareStats>>> GetSoftwareStatsAsync() =>
            ApiGet<ImmutableDictionary<string, SoftwareStats>>($"{TaskManagerEndpoint}/getsoftwarestats", "stats");


        public interface IHasTotal { ulong Total { get; } }
        public record SoftwareStats(ulong Total, ImmutableDictionary<string, SoftwareStatsByVersion> ByVersion) : IHasTotal;
        public record SoftwareStatsByVersion(ulong Total) : IHasTotal; // TODO: byplugin
    }
}