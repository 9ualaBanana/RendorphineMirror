using System.Net;

namespace Node.Listeners;

public record NodeFileInfo(string FileName, long Size);
public class WatcherTaskListener : ListenerBase
{
    protected override bool IsLocal => false;
    protected override bool RequiresAuthentication => true;
    protected override string? Prefix => "watcher";

    protected override ValueTask Execute(HttpListenerContext context)
    {
        var query = context.Request.QueryString;

        var dir = query["dir"];
        if (dir is null)
        {
            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            context.Response.Close();
            return ValueTask.CompletedTask;
        }

        context.Response.StatusCode = (int) HttpStatusCode.OK;
        Directory.CreateDirectory(dir);

        var writer = new LocalPipe.Writer(context.Response.OutputStream);
        var watcher = new FileSystemWatcher(dir) { IncludeSubdirectories = true };
        watcher.Created += (obj, e) =>
        {
            if (!File.Exists(e.FullPath)) return;

            var filename = Path.GetFileName(e.FullPath);
            var info = new UserTaskInputInfo(e.FullPath);

            writer.WriteAsync(new NodeFileInfo(e.FullPath, new FileInfo(e.FullPath).Length))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        watcher.Dispose();
                        throw t.Exception ?? new Exception();
                    }
                });
        };

        watcher.EnableRaisingEvents = true;


        return ValueTask.CompletedTask;
    }
}
