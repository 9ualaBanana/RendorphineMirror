using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public abstract class ListenerBase
{
    protected virtual string? Prefix => null;
    protected virtual bool IsLocal => true;
    protected virtual int Port => Settings.LocalListenPort;

    protected readonly HttpListener Listener = new();

    public void Start()
    {
        var prefix = $"http://{(IsLocal ? "127.0.0.1" : "+")}:{Port}/{Prefix}";
        if (!prefix.EndsWith("/")) prefix += "/";

        Log.Information($"Starting HTTP listener on {prefix}");
        Listener.Prefixes.Add(prefix);
        Listener.Start();

        new Thread(async () =>
        {
            while (true)
            {
                try
                {
                    var context = Listener.GetContext();
                    LogRequest(context.Request);
                    await Execute(context);
                }
                catch (Exception ex) { Log.Error(ex.ToString()); }
            }
        })
        { IsBackground = true }.Start();
    }

    protected abstract ValueTask Execute(HttpListenerContext context);



    protected static JsonSerializerSettings JsonSettingsWithTypes => LocalApi.JsonSettingsWithType;
    protected static JsonSerializer JsonSerializerWithTypes => LocalApi.JsonSerializerWithType;

    protected static void LogRequest(HttpListenerRequest request) => Log.Verbose(@$"{request.RemoteEndPoint} {request.HttpMethod} {request.RawUrl}");
    protected static JObject JsonFromOpResult(in OperationResult result)
    {
        var json = new JObject() { ["ok"] = new JValue(result.Success), };
        if (!result) json["errormsg"] = result.AsString();

        return json;
    }
    protected static JObject JsonFromOpResult<T>(in OperationResult<T> result)
    {
        var json = JsonFromOpResult(result.EString);
        if (result) json["value"] = JToken.FromObject(result.Value!);

        return json;
    }
    protected static JObject JsonFromOpResult(JToken token)
    {
        var json = JsonFromOpResult((OperationResult) true);
        json["value"] = token;

        return json;
    }
    protected static Task<HttpStatusCode> WriteSuccess(HttpListenerResponse response) => _Write(response, JsonFromOpResult((OperationResult) true));
    protected static Task<HttpStatusCode> WriteJson<T>(HttpListenerResponse response, in OperationResult<T> result) => _Write(response, JsonFromOpResult(result));
    protected static Task<HttpStatusCode> WriteJson(HttpListenerResponse response, in OperationResult result) => _Write(response, JsonFromOpResult(result));
    protected static Task<HttpStatusCode> WriteJToken(HttpListenerResponse response, JToken json) => _Write(response, JsonFromOpResult(json));

    protected static async Task<HttpStatusCode> _Write(HttpListenerResponse response, JObject json, HttpStatusCode code = HttpStatusCode.OK)
    {
        using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
        using var jwriter = new JsonTextWriter(writer) { CloseOutput = false };
        await json.WriteToAsync(jwriter).ConfigureAwait(false);

        return code;
    }

    protected static Task<HttpStatusCode> WriteErr(HttpListenerResponse response, string text) => WriteJson(response, OperationResult.Err(text));
    protected static Task<HttpStatusCode> WriteText(HttpListenerResponse response, string text, HttpStatusCode code = HttpStatusCode.OK) => Write(response, Encoding.UTF8.GetBytes(text), code);

    protected static async Task<HttpStatusCode> Write(HttpListenerResponse response, ReadOnlyMemory<byte> bytes, HttpStatusCode code = HttpStatusCode.OK)
    {
        await response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        return code;
    }
    protected static async Task<HttpStatusCode> Write(HttpListenerResponse response, Stream bytes, HttpStatusCode code = HttpStatusCode.OK)
    {
        await bytes.CopyToAsync(response.OutputStream).ConfigureAwait(false);
        return code;
    }


    protected static Task<HttpStatusCode> WriteNoArgument(HttpListenerResponse response, string key) => WriteErr(response, "no " + key);
    protected static Task<HttpStatusCode> Test(HttpListenerRequest request, HttpListenerResponse response, string c1, Func<string, Task<HttpStatusCode>> func)
    {
        var c1v = request.QueryString[c1];
        if (c1v is null) return WriteNoArgument(response, c1);

        return func(c1v);
    }
    protected static Task<HttpStatusCode> Test(HttpListenerRequest request, HttpListenerResponse response, string c1, string c2, Func<string, string, Task<HttpStatusCode>> func) =>
        Test(request, response, c1, c1v =>
        {
            var c2v = request.QueryString[c2];
            if (c2v is null) return WriteNoArgument(response, c2);

            return func(c1v, c2v);
        });
}
