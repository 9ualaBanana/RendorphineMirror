using System.IO.Compression;
using System.Net;
using System.Web;

namespace Node.Listeners
{
    public class PublicPagesListener : ExecutableListenerBase
    {
        protected override ListenTypes ListenType => ListenTypes.WebServer;

        public required ICompletedTasksStorage CompletedTasks { get; init; }
        public required IWatchingTasksStorage WatchingTasks { get; init; }
        public required DataDirs Dirs { get; init; }

        static string[] imagesExtentions = { ".jpg", ".jpeg", ".png" };

        public PublicPagesListener(ILogger<PublicPagesListener> logger) : base(logger) { }

        protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
        {
            var images = new List<string>();
            var source = WatchingTasks.WatchingTasks.Values
                .Select(d => d.Source)
                .OfType<OneClickWatchingTaskInputInfo>()
                .First();

            foreach (var outdir in Directory.GetDirectories(source.ResultDirectory))
            {
                try
                {
                    var files = Directory.GetFiles(Path.Combine(outdir, "renders"), "*.png");
                    images.AddRange(files);
                }
                catch { }
            }


            var request = context.Request;
            var response = context.Response;


            if (path == "gallery")
            {
                string now = DateTime.Now.Ticks.ToString();
                string info = $@"
                <!doctype html>
                <html lang=""en"">

                <head>
                    <meta charset=""UTF-8"" />
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
                    <title>Vite + React + TS</title>
                    <script>
                        const loadResource = (commitHash, isStyle = false) => {{
                            if (isStyle) {{
                                const link = document.createElement('link');
                                link.href = `https://cdn.jsdelivr.net/gh/slavamirniy/ocgallery@${{commitHash}}/dist/assets/index.css`;
                                link.rel = 'stylesheet';
                                document.head.appendChild(link);
                            }} else {{
                                const script = document.createElement('script');
                                script.src = `https://cdn.jsdelivr.net/gh/slavamirniy/ocgallery@${{commitHash}}/dist/assets/index.js`;
                                script.type = 'module';
                                document.body.appendChild(script);
                            }}
                        }};

                        fetch('https://api.github.com/repos/slavamirniy/ocgallery/commits/main')
                            .then(response => response.json())
                            .then(data => {{
                                const commitHash = data.sha;
                                loadResource(commitHash); // Загрузка скрипта
                                loadResource(commitHash, true); // Загрузка стилей
                            }});
                    </script>
                </head>

                <body>
                    <div id=""root""></div>
                </body>

                </html>";

                using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                writer.Write(info);
                return HttpStatusCode.OK;
            }

            if (path.StartsWith("getocfile"))
            {
                var fileIndexString = HttpUtility.ParseQueryString(context.Request.Url.ThrowIfNull().Query)["file"].ThrowIfNull();
                if (fileIndexString == null || !int.TryParse(fileIndexString, out int fileIndex) || fileIndex >= images.Count) return HttpStatusCode.NotFound;
                using var filestream = File.OpenRead(images[fileIndex]);
                response.ContentLength64 = filestream.Length;

                await filestream.CopyToAsync(response.OutputStream);
                return HttpStatusCode.OK;
            }

            if (path == "helloworld")
            {
                using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                writer.Write("Hello world, epta");

                return HttpStatusCode.OK;
            }

            if (path == "logs")
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
            }

            if (path == "")
            {
                using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                writer.Write("<a href='/gallery'>Gallery</a><br><a href='/logs'>Logs</a>");
                return HttpStatusCode.OK;
            }

            return HttpStatusCode.NotFound;
        }
    }
}
