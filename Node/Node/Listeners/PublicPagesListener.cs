using System.DirectoryServices;
using System.IO.Compression;
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


            string getPageScript(string username, string path)
            {
                path = path.Replace("/", string.Empty);
                if (string.IsNullOrWhiteSpace(path))
                    path = "index";

                return $$"""
                <!doctype html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8" />
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>Vite + React + TS</title>
                    <script>
                        const loadResource = (commitHash, isStyle = false) => {
                            if (isStyle) {
                                const link = document.createElement('link');
                                link.href = `https://cdn.jsdelivr.net/gh/{{username}}/{{path}}@${commitHash}/dist/assets/index.css`;
                                link.rel = 'stylesheet';
                                document.head.appendChild(link);
                            } else {
                                const script = document.createElement('script');
                                script.src = `https://cdn.jsdelivr.net/gh/{{username}}/{{path}}@${commitHash}/dist/assets/index.js`;
                                script.type = 'module';
                                document.body.appendChild(script);
                            }
                        };
                        fetch('https://api.github.com/repos/{{username}}/{{path}}/commits/main')
                            .then(response => response.json())
                            .then(data => {
                                const commitHash = data.sha;
                                loadResource(commitHash); // Загрузка скрипта
                                loadResource(commitHash, true); // Загрузка стилей
                            });
                    </script>
                </head>
                <body>
                    <div id="root"></div>
                </body>
                </html>
                """;
            }

            if (path.Length == 0 || path.StartsWith("rf/", StringComparison.Ordinal))
            {
                using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                writer.Write(getPageScript("rfpages", path));

                return HttpStatusCode.OK;
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
                return await WriteJson(response, RFProducts.RFProducts.Select(p => KeyValuePair.Create(p.Key, rfProductToJson(p.Value))).ToImmutableDictionary().AsOpResult());

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
                    .ThrowIfNull("Unknown type").Value.ToObject<FileWithFormat>().ThrowIfNull().Path;

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

            if (path == "getmynodesips")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    var result = await Api.GetMyNodesAsync()
                        .Next(nodes => nodes.Select(n => $"{n.Info.Ip}:{n.Info.Port}").ToArray().AsOpResult());

                    return await WriteJson(response, result);
                });
            }

            if (path == "logs")
            {
                return await CheckSendAuthentication(context, async () =>
                {
                    string logDir = "logs";
                    string? q = context.Request.QueryString["id"];
                    string info = "";

                    if (q == null)
                    {
                        addFolder(logDir);


                        void addFolder(string folder)
                        {
                            string[] files = Directory.GetFiles(folder);
                            info += $"<b style='font-size: 32px'>{Path.GetFileName(folder)}</b></br>";
                            foreach (string file in files)
                            {
                                info += $"<a href='/logs?id={Path.GetRelativePath(logDir, file)}'>{Path.GetFileName(file)}</a></br>";
                            }

                            info += "<div style=\"margin-left:4px\">";
                            foreach (string f in Directory.GetDirectories(folder))
                                addFolder(f);
                            info += "</div>";
                        }
                    }
                    else
                    {
                        string filepath = Path.Combine(logDir, q);
                        if (!Path.GetFullPath(filepath).StartsWith(logDir, StringComparison.Ordinal))
                            return HttpStatusCode.NotFound;

                        response.Headers["Content-Encoding"] = "gzip";

                        using Stream file = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        using var gzip = new GZipStream(response.OutputStream, CompressionLevel.Fastest);
                        await file.CopyToAsync(gzip);

                        return HttpStatusCode.OK;
                    }

                    using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                    writer.Write(info);

                    return HttpStatusCode.OK;
                });
            }

            return HttpStatusCode.NotFound;
        }

        protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (path == "loginas")
            {
                return await TestPost(await CachedHttpListenerRequest.Create(request), response, "email", "password", async (email, password) =>
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
                return await TestPost(await CachedHttpListenerRequest.Create(request), response, "sid", async (sid) =>
                    await WriteJson(response, sid.AsOpResult())
                );
            }

            return await base.ExecutePost(path, context);
        }
    }
}
