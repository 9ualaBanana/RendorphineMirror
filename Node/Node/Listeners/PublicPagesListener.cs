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
        public string oneClickFilesPath = "";

        static string[] imagesExtentions = { ".jpg", ".jpeg", ".png" };

        public PublicPagesListener(ILogger<PublicPagesListener> logger) : base(logger) { }

        protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
        {
            if (string.IsNullOrEmpty(oneClickFilesPath))
            {
                var source = WatchingTasks.WatchingTasks.Values
                    .Select(d => d.Source)
                    .OfType<OneClickWatchingTaskInputInfo>()
                    .FirstOrDefault();

                if (source != null)
                {
                    var dir = Path.Combine(source.OutputDirectory, "unity", "Assets");

                    var paath = Directory.GetFiles(dir)
                        .SingleOrDefault(file => Path.GetExtension(file) == ".fbx");

                    if (paath is not null)
                        paath = Path.ChangeExtension(paath, null);
                    else
                        paath = Directory.GetDirectories(dir)
                            .SingleOrDefault(dir => Path.GetFileName(dir) != "OneClickImport");

                    if (paath != null)
                        oneClickFilesPath = Path.Combine(paath, "renders");
                }
            }

            await Task.Delay(0); // to hide a warning

            var request = context.Request;
            var response = context.Response;


            if (path == "gallery")
            {
                string? pageString = context.Request.QueryString["days"];
                int page;

                if (pageString == null || !int.TryParse(pageString, out page)) page = 0;

                string info = "<html><body>";
                info += $"<form method='get'>Страница: <input type ='number' name='days' value={page + 1}><input type = 'submit' value = 'Перейти'></form>";
                info += $"<b>Page: {page} </b><br>";

                string[] files = { };
                if (oneClickFilesPath.Length > 0)
                    files = Directory.GetFiles(oneClickFilesPath);

                foreach (var file in files)
                {
                    info += $"<img width='200px' src='./getocfile/{file}'>";
                    info += "</br>";
                }

                info += "</body></html>";

                using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                writer.Write(info);
                return HttpStatusCode.OK;
            }

            if (path.StartsWith("getocfile"))
            {
                if (oneClickFilesPath.Length == 0) return HttpStatusCode.OK;
                string name = Path.GetFileName(path);
                string filename = Path.Combine(oneClickFilesPath, name);
                using var filestream = File.OpenRead(filename);
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
