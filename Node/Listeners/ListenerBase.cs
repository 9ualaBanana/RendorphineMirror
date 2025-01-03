using System.Collections.Specialized;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public abstract class ListenerBase
{
    protected readonly Logger _logger = LogManager.GetCurrentClassLogger();

    protected virtual string? Prefix => null;
    protected abstract ListenTypes ListenType { get; }
    protected virtual bool RequiresAuthentication => false;

    protected readonly HttpListener Listener = new();

    public void Start()
    {
        if (ListenType.HasFlag(ListenTypes.Local)) addprefix($"127.0.0.1:{Settings.LocalListenPort}");
        if (ListenType.HasFlag(ListenTypes.Public)) addprefix($"+:{PortForwarding.Port}");
        if (ListenType.HasFlag(ListenTypes.WebServer)) addprefix($"+:{PortForwarding.ServerPort}");
        void addprefix(string prefix)
        {
            prefix = $"http://{prefix}/{Prefix}";
            if (!prefix.EndsWith("/")) prefix += "/";

            Listener.Prefixes.Add(prefix);
        }

        _logger.Info($"Starting HTTP {GetType().Name} on {string.Join(", ", Listener.Prefixes)}");
        Listener.Start();

        new Thread(() =>
        {
            while (true)
            {
                try
                {
                    var context = Listener.GetContext();
                    LogRequest(context.Request);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (!context.Request.IsLocal && RequiresAuthentication && !(await CheckAuthentication(context)))
                            {
                                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                                context.Response.Close();
                                return;
                            }

                            await Execute(context);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex.ToString());
                            await WriteErr(context.Response, ex.Message).ConfigureAwait(false);

                            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                            context.Response.Close();
                        }
                    });
                }
                catch (Exception ex) { _logger.Error(ex.ToString()); }
            }
        })
        { IsBackground = true }.Start();
    }

    protected abstract ValueTask Execute(HttpListenerContext context);

    protected async Task<bool> CheckAuthentication(HttpListenerContext context)
    {
        var sid = context.Request.QueryString["sessionid"];
        return sid is not null && await CheckAuthentication(sid);
    }
    static readonly Dictionary<string, bool> CachedAuthentications = new();

    /// <summary> Returns true if provided sessionid is also from ours user </summary>
    static async ValueTask<bool> CheckAuthentication(string sid)
    {
        var check = await docheck();
        CachedAuthentications[sid] = check;

        return check;


        async ValueTask<bool> docheck()
        {
            var oursid = Settings.SessionId;
            if (sid == oursid) return true;
            if (CachedAuthentications.TryGetValue(sid, out var cached))
                return cached;

            var nodes = await Apis.GetMyNodesAsync(sid).ConfigureAwait(false);
            if (!nodes) return false;

            var theiruserid = nodes.Result.Select(x => x.UserId).FirstOrDefault();
            if (theiruserid is null) return false;

            var myuserid = Settings.UserId;
            return myuserid == theiruserid;
        }
    }

    protected string GetPath(HttpListenerContext context)
    {
        if (context.Request.Url is null) throw new InvalidOperationException();

        var response = context.Response;

        var path = context.Request.Url.LocalPath.Substring((Prefix?.Length ?? 0) + 1);
        if (path == "") return path;
        if (path[0] == '/') path = path[1..];
        if (path[^1] == '/') path = path[..^1];

        return path;
    }


    protected static JsonSerializerSettings JsonSettingsWithTypes => LocalApi.JsonSettingsWithType;
    protected static JsonSerializer JsonSerializerWithTypes => LocalApi.JsonSerializerWithType;

    protected void LogRequest(HttpListenerRequest request) => _logger.Trace(@$"{request.RemoteEndPoint} {request.HttpMethod} {request.RawUrl}");
    protected static Task<HttpStatusCode> WriteSuccess(HttpListenerResponse response) => _Write(response, JsonApi.Success());
    protected static Task<HttpStatusCode> WriteJson<T>(HttpListenerResponse response, in OperationResult<T> result) => _Write(response, JsonApi.JsonFromOpResult(result));
    protected static Task<HttpStatusCode> WriteJson(HttpListenerResponse response, in OperationResult result) => _Write(response, JsonApi.JsonFromOpResult(result));
    protected static Task<HttpStatusCode> WriteJToken(HttpListenerResponse response, JToken json) => _Write(response, JsonApi.JsonFromOpResult(json));

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
    protected static Task<HttpStatusCode> Test(HttpListenerRequest request, HttpListenerResponse response, string c1, string c2, string c3, Func<string, string, string, Task<HttpStatusCode>> func) =>
        Test(request, response, c1, c2, (c1v, c2v) =>
        {
            var c3v = request.QueryString[c3];
            if (c3v is null) return WriteNoArgument(response, c3);

            return func(c1v, c2v, c3v);
        });
    protected static Task<HttpStatusCode> Test(HttpListenerRequest request, HttpListenerResponse response, string c1, string c2, string c3, string c4, Func<string, string, string, string, Task<HttpStatusCode>> func) =>
        Test(request, response, c1, c2, c3, (c1v, c2v, c3v) =>
        {
            var c4v = request.QueryString[c4];
            if (c4v is null) return WriteNoArgument(response, c4);

            return func(c1v, c2v, c3v, c4v);
        });


    protected static OperationResult<string> ReadQueryValue(HttpListenerContext context, string key) => ReadQueryString(context.Request.QueryString, key);
    protected static OperationResult<string> ReadQueryString(NameValueCollection query, string key)
    {
        var val = query[key];
        if (val is null) return OperationResult.Err($"{key} was not provided");

        return val;
    }

    protected static OperationResult<long> ReadQueryLong(NameValueCollection query, string key, long? def = null) =>
        ReadQueryString(query, key)
            .Next(str =>
            {
                if (!long.TryParse(str, out var value))
                    return def?.AsOpResult() ?? OperationResult.Err($"{str} is not a valid long value");

                return value.AsOpResult();
            });

    protected static string GetQueryPart(StringValues values, string name) => string.Join(" ", values).Split(name + "=")[1].Split(";")[0];


    [Flags]
    protected enum ListenTypes
    {
        Local = 1 << 0,
        Public = 1 << 1,
        WebServer = 1 << 2,
    }

    protected class CachedMultipartReader : IAsyncEnumerable<MultipartSection>, IDisposable
    {
        readonly string TempFileName, Boundary;
        readonly List<IDisposable> ToDispose = new();

        private CachedMultipartReader(string tempfile, string boundary)
        {
            TempFileName = tempfile;
            Boundary = boundary;
        }

        public static async Task<CachedMultipartReader> Create(string boundary, Stream source)
        {
            var tempfile = Path.GetTempFileName();
            using (var file = File.OpenWrite(tempfile))
                await source.CopyToAsync(file);

            return new(tempfile, boundary);
        }

        public async Task<Dictionary<string, MultipartSection>> GetSectionsAsync()
        {
            var result = new Dictionary<string, MultipartSection>();
            await foreach (var section in this)
                result.Add(GetQueryPart(section.ContentDisposition, "name"), section);

            return result;
        }
        public async IAsyncEnumerator<MultipartSection> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            for (int i = 1; true; i++)
            {
                var stream = File.OpenRead(TempFileName);
                var reader = new MultipartReader(Boundary, stream);
                var section = null as MultipartSection;
                for (int j = 0; j < i; j++)
                    section = await reader.ReadNextSectionAsync();

                if (section is null || section.ContentDisposition is null)
                {
                    stream.Dispose();
                    break;
                }

                ToDispose.Add(stream);
                ToDispose.Add(section.Body);

                yield return section;
            }
        }

        public void Dispose()
        {
            if (File.Exists(TempFileName))
                File.Delete(TempFileName);

            foreach (var disposables in ToDispose)
                try { disposables.Dispose(); }
                catch { }
        }
    }
}
