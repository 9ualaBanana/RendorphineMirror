using System.Net;

namespace Node.Listeners;

public class NodeStateListener : ListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local;
    protected override string Prefix => "getstate";

    public NodeStateListener(ILogger<NodeStateListener> logger) : base(logger) { }

    protected override ValueTask Execute(HttpListenerContext context)
    {
        context.Response.StatusCode = (int) HttpStatusCode.OK;

        new Thread(async () =>
        {
            var handle = new EventWaitHandle(false, EventResetMode.AutoReset);
            var changed = null as string;
            NodeGlobalState.Instance.AnyChanged.Subscribe(handle, v => { changed = v; handle.Set(); });

            var writer = new LocalPipe.Writer(context.Response.OutputStream);
            while (true)
            {
                try
                {
                    JObject json;
                    if (changed is null) json = JObject.FromObject(NodeGlobalState.Instance, JsonSettings.TypedS);
                    else json = new JObject() { [changed] = JToken.FromObject(typeof(NodeGlobalState).GetField(changed)!.GetValue(NodeGlobalState.Instance)!, JsonSettings.TypedS), };

                    var wrote = await writer.WriteAsync(json).ConfigureAwait(false);
                    if (!wrote) return;
                }
                catch
                {
                    await Task.Delay(1000);
                    continue;
                }

                handle.WaitOne();
            }
        })
        { IsBackground = true }.Start();

        return ValueTask.CompletedTask;
    }
}
