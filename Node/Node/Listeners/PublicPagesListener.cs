using System.Net;
using System.Text;
using System.Web;

namespace Node.Listeners
{
    public class PublicPagesListener : ExecutableListenerBase
    {
        protected override ListenTypes ListenType => ListenTypes.WebServer;

        public required Apis Api { get; init; }
        public required ICompletedTasksStorage CompletedTasks { get; init; }
        public required IWatchingTasksStorage WatchingTasks { get; init; }
        public required IRFProductStorage RFProducts { get; init; }
        public required DataDirs Dirs { get; init; }

        public PublicPagesListener(ILogger<PublicPagesListener> logger) : base(logger) { }

        protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
        {
            var source = WatchingTasks.WatchingTasks.Values
                .Select(d => d.Source)
                .OfType<OneClickWatchingTaskInputInfo>()
                .FirstOrDefault();

            var request = context.Request;
            var response = context.Response;

            if (source is not null)
            {
                var maxlogs = new List<string>();
                var unitylogs = new List<string>();
                var unitylogs2 = new List<string>();
                try { maxlogs.AddRange(Directory.GetFiles(source.LogDirectory)); }
                catch { }
                try { unitylogs.AddRange(Directory.GetFiles(Path.Combine(source.LogDirectory, "unity"))); }
                catch { }

                try
                {
                    foreach (var dir in Directory.GetDirectories(Path.Combine(source.OutputDirectory)))
                    {
                        foreach (var ddir in Directory.GetDirectories(Path.Combine(dir, "unity", "Assets")))
                        {
                            var productName = Path.GetFileName(ddir);
                            if (File.Exists(Path.Combine(ddir, productName + ".log")))
                                unitylogs2.Add(Path.Combine(ddir, productName + ".log"));
                        }
                    }
                }
                catch { }


                if (path == "getoclogs")
                {
                    return await CheckSendAuthentication(context, async () =>
                    {
                        var resp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { maxlogs, unitylogs, unitylogs2 }));
                        response.ContentLength64 = resp.Length;
                        await response.OutputStream.WriteAsync(resp);

                        return HttpStatusCode.OK;
                    });
                }

                if (path == "getoclog")
                {
                    return await CheckSendAuthentication(context, async () =>
                    {
                        var fileIndexString = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query)["file"].ThrowIfNull();
                        var type = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query)["type"].ThrowIfNull();

                        if (fileIndexString == null || !int.TryParse(fileIndexString, out int fileIndex)) return HttpStatusCode.NotFound;


                        var items = type switch
                        {
                            "max" => maxlogs,
                            "unity" => unitylogs,
                            "unity2" => unitylogs2,
                            _ => throw new Exception("Unknown type")
                        };
                        var item = items[fileIndex];

                        var filename = Encoding.UTF8.GetBytes(item + "\n");
                        using var filestream = File.OpenRead(item);
                        response.ContentLength64 = filestream.Length + filename.Length;

                        await response.OutputStream.WriteAsync(filename);
                        await filestream.CopyToAsync(response.OutputStream);
                        return HttpStatusCode.OK;
                    });
                }
            }

            static JObject rfProductToJson(RFProduct product)
            {
                return new JObject()
                {
                    ["id"] = product.ID,
                    ["type"] = product.Type,
                    ["subproducts"] = new JArray(product.SubProducts.Select(rfProductToJson).ToArray()),
                };
            }

            if (path == "getocproducts")
            {
                response.AddHeader("Access-Control-Allow-Origin", "*");
                return await WriteJson(response, RFProducts.RFProducts.Select(p => KeyValuePair.Create(p.Key, rfProductToJson(p.Value))).ToImmutableDictionary().AsOpResult());
            }

            if (path == "getocproductdata")
            {
                var id = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query)["id"];
                if (id is null || !RFProducts.RFProducts.TryGetValue(id, out var rfp)) return HttpStatusCode.NotFound;

                return await WriteJson(response, rfProductToJson(rfp).AsOpResult());
            }

            if (path == "getocproductqsp")
            {
                var query = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query);
                var id = query["id"];
                var type = query["type"];

                if (id is null || !RFProducts.RFProducts.TryGetValue(id, out var product))
                    return await WriteErr(response, "Unknown product");

                if (type is not ("imagefooter" or "imageqr" or "video"))
                    return await WriteErr(response, "Unknown type");

                var filepath = JObject.FromObject(product.QSPreview).Property(type, StringComparison.OrdinalIgnoreCase)
                    .ThrowIfNull("Type is not present on file").Value.ToObject<FileWithFormat>().ThrowIfNull().Path;

                using var file = File.OpenRead(filepath);
                response.StatusCode = (int) HttpStatusCode.OK;
                response.ContentLength64 = file.Length;
                await file.CopyToAsync(response.OutputStream);

                return HttpStatusCode.OK;
            }

            // authenticated

            if (path == "getocproductfile")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    var query = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query);
                    var id = query["id"].ThrowIfNull();

                    if (!RFProducts.RFProducts.TryGetValue(id, out var product))
                        return await WriteErr(response, "Unknown product");

                    var filepath = product.Idea.Path;

                    using var file = File.OpenRead(filepath);
                    response.StatusCode = (int) HttpStatusCode.OK;
                    response.ContentLength64 = file.Length;
                    await file.CopyToAsync(response.OutputStream);

                    return HttpStatusCode.OK;
                });
            }
            if (path == "getocproductdir")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    var query = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query);
                    var id = query["id"].ThrowIfNull();

                    if (!RFProducts.RFProducts.TryGetValue(id, out var product))
                        return await WriteErr(response, "Unknown product");

                    var dirpath = Path.GetFullPath(product.Idea.Path);
                    var result = new JArray(Directory.GetFiles(dirpath, "*", SearchOption.AllDirectories).Select(p => Path.GetRelativePath(dirpath, p)).ToArray());

                    return await WriteJson(response, result.AsOpResult());
                });
            }
            if (path == "getocproductdirfile")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    var query = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query);
                    var id = query["id"].ThrowIfNull();
                    var filename = query["file"].ThrowIfNull();

                    if (!RFProducts.RFProducts.TryGetValue(id, out var product))
                        return await WriteErr(response, "Unknown product");

                    var filepath = Path.GetFullPath(Path.Combine(product.Idea.Path, filename));
                    if (!filepath.StartsWith(Path.GetFullPath(product.Idea.Path), StringComparison.Ordinal))
                        return HttpStatusCode.NotFound;

                    using var file = File.OpenRead(filepath);
                    response.StatusCode = (int) HttpStatusCode.OK;
                    response.ContentLength64 = file.Length;
                    await file.CopyToAsync(response.OutputStream);

                    return HttpStatusCode.OK;
                });
            }

            if (path == "getmynodesips")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    var result = await Api.GetMyNodesAsync()
                        .Next(nodes => nodes.Select(n => $"{n.Info.Ip}:{n.Info.Port}").ToArray().AsOpResult());

                    return await WriteJson(response, result);
                });
            }

            using var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5336/" + path);
            foreach (var header in request.Headers.AllKeys)
                if (header is not null)
                    message.Headers.Add(header, request.Headers[header]);

            var call = await Api.Api.Client.SendAsync(message);
            await call.Content.CopyToAsync(response.OutputStream);
            return call.StatusCode;
        }

        protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context, Stream inputStream)
        {
            var response = context.Response;

            if (path == "loginas")
            {
                return await TestPost(await CreateCached(inputStream), response, "email", "password", async (email, password) =>
                {
                    var result = await Api.Api.ApiPost<SessionManager.LoginResult>($"{(global::Common.Api.TaskManagerEndpoint)}/login", null, "Logging in", ("email", email), ("password", password), ("lifetime", TimeSpan.FromDays(1).TotalMilliseconds.ToString()), ("guid", Guid.NewGuid().ToString()));

                    // sessionid of another account
                    if (result.Success && result.Value.UserId != Settings.UserId)
                    {
                        var nodes = await Api.WithSessionId(result.Value.SessionId).GetMyNodesAsync();
                        if (!nodes)
                            return await WriteErr(response, "Unknown server error");

                        if (nodes.Value.Length == 0)
                            return await WriteErr(response, "Account has no nodes");

                        var node = null as NodeInfo;
                        foreach (var n in nodes.Value)
                        {
                            try
                            {
                                var online = (await Api.Api.Client.GetAsync($"http://{n.Ip}:{n.Info.Port}/ping", new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token)).IsSuccessStatusCode;
                                if (!online) continue;

                                node = n;
                                break;
                            }
                            catch { }
                        }
                        if (node is null)
                            return await WriteErr(response, "No online nodes found on that account");

                        response.Headers.Set("Location", $"http://{node.Ip}:{node.Info.WebPort}/loginas");
                        return HttpStatusCode.Redirect;
                    }

                    return await WriteJson(response, result);
                });
            }
            if (path == "loginassid")
            {
                return await TestPost(await CreateCached(inputStream), response, "sid", async (sid) =>
                    await WriteJson(response, sid.AsOpResult())
                );
            }

            using var message = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5336/" + path);
            message.Content = new StreamContent(inputStream);

            var call = await Api.Api.Client.SendAsync(message);
            await call.Content.CopyToAsync(response.OutputStream);
            return call.StatusCode;
        }
    }
}
