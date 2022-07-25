using System.Net;

namespace Node.Listeners;

public class NodeStateListener : ListenerBase
{
    protected override string Prefix => "getstate";

    protected override ValueTask Execute(HttpListenerContext context)
    {
        context.Response.StatusCode = (int) HttpStatusCode.OK;

        new Thread(async () =>
        {
            var handle = new EventWaitHandle(false, EventResetMode.AutoReset);
            NodeGlobalState.Instance.AnyChanged.Subscribe(handle, () => handle.Set());

            var writer = new LocalPipe.Writer(context.Response.OutputStream);
            while (true)
            {
                var wrote = await writer.WriteAsync(NodeGlobalState.Instance).ConfigureAwait(false);
                if (!wrote) return;

                handle.WaitOne();
            }
        })
        { IsBackground = true }.Start();

        return ValueTask.CompletedTask;
    }
}
