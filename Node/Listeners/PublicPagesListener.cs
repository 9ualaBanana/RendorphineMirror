using System.Net;

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
                    .Where(t => t.Value.StartTime >= DateTime.Now.AddDays(-1 * days)
                        && imagesExtentions.Contains(Path.GetExtension(t.Value.TaskInfo.FSOutputFile())));

                foreach (var task in filteredTasks)
                {
                    info += $"<img width='200px' src='./getfile/{task.Key}'>";
                    info += $"<details><p>ID:{task.Value.TaskInfo.Id}<br>Start:{task.Value.StartTime.ToString()}<br>Finish:{task.Value.FinishTime}<p></details>";
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
                string file = tasks[taskId].TaskInfo.FSOutputFile();
                if (File.Exists(file))
                {
                    using var writer = new BinaryWriter(response.OutputStream, System.Text.Encoding.Default, leaveOpen: true);
                    writer.Write(File.ReadAllBytes(file));
                    return HttpStatusCode.OK;
                }
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
                string[] folders = Directory.GetDirectories(logDir);
                string? q = context.Request.QueryString["id"];
                string info = "";

                if (q == null || !int.TryParse(q, out _))
                {
                    int i = 0;
                    foreach (string folder in folders)
                    {
                        string[] files = Directory.GetFiles(folder);
                        info += $"<b style='font-size: 32px'>{Path.GetFileName(folder)}</b></br>";
                        foreach (string file in files)
                        {
                            info += $"<a href='/logs?id={i++}'>{Path.GetFileName(file)}</a></br>";
                        }
                    }
                }
                else
                {
                    int i = 0;
                    int id = int.Parse(q);
                    foreach (string folder in folders)
                    {
                        string[] files = Directory.GetFiles(folder);
                        if (i + files.Length - 1 >= id)
                        {
                            info = File.ReadAllText(files[id - i]);
                            break;
                        }
                        i += files.Length;
                    }
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
