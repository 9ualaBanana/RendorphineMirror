using System.Net;

namespace Node.Listeners;

public class NodeStateListener : ListenerBase
{
    public event Action<NodeStateSender.Instance>? OnReceive;

    protected override ListenTypes ListenType => ListenTypes.WebServer | ListenTypes.Local;
    protected override string Prefix => "getstate";

    public required NodeStateSender Sender { get; init; }

    public NodeStateListener(ILogger<NodeStateListener> logger) : base(logger) { }

    protected override ValueTask Execute(HttpListenerContext context)
    {
        context.Response.StatusCode = (int) HttpStatusCode.OK;
        Sender.SendingLoop(context.Response.OutputStream)
            .Consume();

        return ValueTask.CompletedTask;
    }
}
