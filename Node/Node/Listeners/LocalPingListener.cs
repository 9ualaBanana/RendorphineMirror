using System.Net;

namespace Node.Listeners;

public class LocalPingListener : ListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local;
    protected override string? Prefix => "ping/";

    public LocalPingListener(ILogger<LocalPingListener> logger) : base(logger) { }

    protected override ValueTask Execute(HttpListenerContext context)
    {
        context.Response.StatusCode = (int) HttpStatusCode.OK;
        context.Response.Close();

        return ValueTask.CompletedTask;
    }
}
