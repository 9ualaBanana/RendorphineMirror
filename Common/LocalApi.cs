using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class LocalApi
    {
        public static readonly JsonSerializerSettings JsonSettingsWithType = new() { TypeNameHandling = TypeNameHandling.Auto, };
        public static readonly JsonSerializer JsonSerializerWithType = JsonSerializer.Create(JsonSettingsWithType);

        public static string LocalIP => $"127.0.0.1:{Settings.LocalListenPort}";
        static readonly HttpClient Client = new();

        static string AddHttp(string url)
        {
            if (!url.StartsWith("http://")) return "http://" + url;
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
                return jv.ToObject<T>(JsonSerializerWithType)!.AsOpResult();
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