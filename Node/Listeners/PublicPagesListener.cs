using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Node.Listeners
{
    public class PublicPagesListener : ExecutableListenerBase
    {
        protected override bool IsLocal => false;

        protected override async Task<HttpStatusCode> ExecuteGet(string path, HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;


            if (path.StartsWith("gallery"))
            {
                string[] fileExt = { "jpg", "jpeg", "png" };
                string taskDir = Init.TaskFilesDirectory;


                if (path == "gallery")
                {
                    IEnumerable<string> foldersWithOutputs = Directory.GetDirectories(taskDir)
                    .Where(d => Directory.Exists(d + "/output") && Directory.EnumerateFiles(d + "/output").Any());
                    var tasksFiles = foldersWithOutputs
                        .GroupBy(folder => fileExt.SelectMany(ext => Directory.EnumerateFiles(folder + "/output", "*." + ext)));

                    string info = "<html><body>";

                    foreach (var task in tasksFiles)
                    {
                        info += $"<br><h3>{Path.GetFileName(task.First())}</h3>";
                        foreach (string file in task.Key)
                        {
                            info += $"<img width='200px' src='./gallery/{Path.GetFileName(task.First()) + "?name=" + Path.GetFileName(file)}'>";
                        }
                    }

                    info += "</body></html>";

                    using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
                    writer.Write(info);
                    return HttpStatusCode.OK;
                }
                else
                {
                    string name = context.Request.QueryString["name"] ?? "";
                    string file = taskDir + "/" + Path.GetFileName(Path.GetFileName(path)) + "/output/" + name;
                    if (File.Exists(file))
                    {
                        using var writer = new BinaryWriter(response.OutputStream, System.Text.Encoding.Default, leaveOpen: true);
                        writer.Write(File.ReadAllBytes(file));
                        return HttpStatusCode.OK;
                    }
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

        protected override async Task<HttpStatusCode> ExecutePost(string path, HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            return HttpStatusCode.NotFound;
        }
    }
}
