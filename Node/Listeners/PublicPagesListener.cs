using System.Net;
using System.Web;

namespace Node.Listeners
{
    public class PublicPagesListener : ExecutableListenerBase
    {
        protected override ListenTypes ListenType => ListenTypes.WebServer;

        static string[] imagesExtentions = { ".jpg", ".jpeg", ".png" };

        protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
        {
            await Task.Delay(0); // to hide a warning

            var request = context.Request;
            var response = context.Response;


            if (path == "gallery")
            {
                string? daysString = context.Request.QueryString["days"];
                int days;

                if (daysString == null || !int.TryParse(daysString, out days)) days = 30;

                string info = "<html><body>";
                info += $"<form method='get'><input type ='number' name='days' value={days}><input type = 'submit'></form>";
                info += $"<b>Files for last {days} days</b><br>";

                var filteredTasks = NodeSettings.CompletedTasks
                    .Where(t => t.Value.StartTime >= DateTime.Now.AddDays(-1 * days));

                foreach (var task in filteredTasks)
                {
                    string[] resultfiles = Directory.GetFiles(task.Value.TaskInfo.FSOutputDirectory());
                    if (resultfiles.Length == 0)
                        info += $"[no files]";

                    foreach (string taskfile in resultfiles.OrderBy(Path.GetExtension))
                    {
                        string taskfilerelative = Path.GetRelativePath(task.Value.TaskInfo.FSOutputDirectory(), taskfile);
                        string mime = MimeTypes.GetMimeType(taskfile);

                        if (mime.Contains("image", StringComparison.Ordinal))
                            info += $"<img width='200px' src='./getfile/{task.Key}?name={HttpUtility.UrlEncode(taskfilerelative)}'>";
                        else if (mime.Contains("video", StringComparison.Ordinal))
                            info += $"<video width='200px' controls><source src='./getfile/{task.Key}?name={HttpUtility.UrlEncode(taskfilerelative)}' type='video/mp4'></video>";
                        // eps?
                    }

                    info += $"<details><p>ID:{task.Value.TaskInfo.Id}<br>Start:{task.Value.StartTime.ToString()}<br>Finish:{task.Value.FinishTime}<p></details><br><br>";
                }

                info += "</body></html>";

                using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                writer.Write(info);
                return HttpStatusCode.OK;
            }

            if (path.StartsWith("getfile"))
            {
                string taskId = Path.GetFileName(path);
                var tasks = NodeSettings.CompletedTasks;
                if (!tasks.ContainsKey(taskId)) return HttpStatusCode.NotFound;

                string? filename = context.Request.QueryString["name"];
                if (filename is null) return HttpStatusCode.NotFound;

                string outputdir = tasks[taskId].TaskInfo.FSOutputDirectory();
                filename = Path.Combine(outputdir, filename);
                if (!filename.StartsWith(outputdir, StringComparison.Ordinal) || !File.Exists(filename))
                    return HttpStatusCode.NotFound;

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
                string logDir = Init.LogDirectory;
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

                    using Stream file = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    response.ContentLength64 = file.Length;
                    await file.CopyToAsync(response.OutputStream);

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
