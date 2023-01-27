using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeCommon
{
    public record LocalApiInstance(ApiInstance Api)
    {
        string Url(string part) => $"http://127.0.0.1:{Settings.LocalListenPort}/{part}";

        public ValueTask<OperationResult<T>> Get<T>(string endpoint, string errorDetails, params (string, string)[] values) =>
            Get<T>(endpoint, "value", errorDetails, values);
        public ValueTask<OperationResult<T>> Post<T>(string endpoint, string errorDetails, params (string, string)[] values) =>
            Post<T>(endpoint, "value", errorDetails, values);
        public ValueTask<OperationResult<T>> Post<T>(string endpoint, string errorDetails, HttpContent content) =>
            Post<T>(endpoint, "value", errorDetails, content);


        public ValueTask<OperationResult<T>> Get<T>(string endpoint, string? property, string errorDetails, params (string, string)[] values) =>
            Api.ApiGet<T>(Url(endpoint), property, errorDetails, values);
        public ValueTask<OperationResult<T>> Post<T>(string endpoint, string? property, string errorDetails, params (string, string)[] values) =>
            Api.ApiPost<T>(Url(endpoint), property, errorDetails, values);
        public ValueTask<OperationResult<T>> Post<T>(string endpoint, string? property, string errorDetails, HttpContent content) =>
            Api.ApiPost<T>(Url(endpoint), property, errorDetails, content);

        public ValueTask<OperationResult> Get(string endpoint, string errorDetails, params (string, string)[] values) =>
            Api.ApiGet(Url(endpoint), errorDetails, values);
        public ValueTask<OperationResult> Post(string endpoint, string errorDetails, params (string, string)[] values) =>
            Api.ApiPost(Url(endpoint), errorDetails, values);
        public ValueTask<OperationResult> Post(string endpoint, string errorDetails, HttpContent content) =>
            Api.ApiPost(Url(endpoint), errorDetails, content);
    }

    public static class LocalApi
    {
        public static readonly LocalApiInstance Default = new LocalApiInstance(Api.Default);

        public static string LocalIP => $"http://127.0.0.1:{Settings.LocalListenPort}";
        static readonly HttpClient Client = new();

        static string AddHttp(string url)
        {
            if (!url.StartsWith("http")) return "http://" + url;
            return url;
        }

        public static ValueTask<OperationResult> Send(string path) => Send(LocalIP, path);
        public static ValueTask<OperationResult<T>> Send<T>(string path) => Send<T>(LocalIP, path);
        public static ValueTask<OperationResult<T>> Send<T>(string path, T _) => Send<T>(LocalIP, path);
        public static ValueTask<OperationResult> Send(string url, string path) => _Send(url, path, () => JustGet(url, path));
        public static ValueTask<OperationResult<T>> Send<T>(string url, string path) => _Send<T>(url, path, () => JustGet(url, path));

        public static ValueTask<OperationResult> Post(string path, HttpContent content) => Post(LocalIP, path, content);
        public static ValueTask<OperationResult<T>> Post<T>(string path, HttpContent content) => Post<T>(LocalIP, path, content);
        public static ValueTask<OperationResult> Post(string url, string path, HttpContent content) => _Send(url, path, () => JustPost(url, path, content));
        public static ValueTask<OperationResult<T>> Post<T>(string url, string path, HttpContent content) => _Send<T>(url, path, () => JustPost(url, path, content));

        public static Task<HttpResponseMessage> JustGet(string url, string path) => Client.GetAsync($"{AddHttp(url)}/{path}");
        public static Task<HttpResponseMessage> JustPost(string url, string path, HttpContent content) => Client.PostAsync($"{AddHttp(url)}/{path}", content);

        static ValueTask<OperationResult> _Send(string url, string path, Func<Task<HttpResponseMessage>> func) =>
            OperationResult.WrapException(async () => (await func().ConfigureAwait(false)).AsOpResult())
            .Next(CheckForErrors)
            .Next(_ => true);
        static ValueTask<OperationResult<T>> _Send<T>(string url, string path, Func<Task<HttpResponseMessage>> func) =>
           OperationResult.WrapException(async () => (await func().ConfigureAwait(false)).AsOpResult())
           .Next(CheckForErrors<T>);



        static OperationResult<T> CheckForErrors<T>(HttpResponseMessage response) =>
            CheckForErrors(response)
            .Next(check => OperationResult.WrapException(() =>
            {
                var jv = check["value"]!;

                if (jv is T t) return t.AsOpResult();
                return jv.ToObject<T>(JsonSettings.TypedS)!.AsOpResult();
            }));
        static OperationResult<JObject> CheckForErrors(HttpResponseMessage response)
        {
            var responsestr = response.Content.ReadAsStringAsync().Result;
            JObject jobject;

            try { jobject = JObject.Parse(responsestr); }
            catch (JsonReaderException) { return OperationResult.Err(@$"HTTP {response.StatusCode}; Message {responsestr}"); }

            var isOk = jobject["ok"];
            if (isOk is null) return OperationResult.Err(response.StatusCode.ToString());
            if (isOk.Value<bool>()) return jobject;

            if (jobject["errormsg"]?.Value<string>() is { } errmsg)
                return OperationResult.Err(errmsg);

            return OperationResult.Err(response.StatusCode.ToString());
        }
    }
}