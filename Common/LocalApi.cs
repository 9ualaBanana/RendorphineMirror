using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common
{
    public static class LocalApi
    {
        static readonly HttpClient Client = new();

        public static ValueTask<OperationResult> Send(string path) =>
            OperationResult.WrapException(async () => (await Client.GetAsync($"http://127.0.0.1:{Settings.LocalListenPort}/{path}").ConfigureAwait(false)).AsOpResult())
            .Next(CheckForErrors)
            .Next(_ => true);
        public static ValueTask<OperationResult<T>> Send<T>(string path) =>
            OperationResult.WrapException(async () => (await Client.GetAsync($"http://127.0.0.1:{Settings.LocalListenPort}/{path}").ConfigureAwait(false)).AsOpResult())
            .Next(CheckForErrors<T>);



        static OperationResult<T> CheckForErrors<T>(HttpResponseMessage response) => CheckForErrors(response).Next(check => OperationResult.WrapException(() => check["value"]!.ToObject<T>()));
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