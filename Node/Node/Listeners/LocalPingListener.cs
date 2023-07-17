using System.Net;

namespace Node.Listeners;

public class LocalPingListener : ListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local;
    protected override string? Prefix => "ping/";

    protected override ValueTask Execute(HttpListenerContext context)
    {
        context.Response.StatusCode = (int) HttpStatusCode.OK;
        context.Response.Close();

        return ValueTask.CompletedTask;
    }
}
