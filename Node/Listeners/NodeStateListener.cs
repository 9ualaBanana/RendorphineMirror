using System.Net;
using Newtonsoft.Json.Linq;

namespace Node.Listeners;

public class NodeStateListener : ListenerBase
{
    protected override ListenTypes ListenType => ListenTypes.Local;
    protected override string Prefix => "getstate";

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
                JObject json;
                if (changed is null) json = JObject.FromObject(NodeGlobalState.Instance, LocalApi.JsonSerializerWithType);
                else json = new JObject() { [changed] = JToken.FromObject(typeof(NodeGlobalState).GetField(changed)!.GetValue(NodeGlobalState.Instance)!, LocalApi.JsonSerializerWithType), };

                var wrote = await writer.WriteAsync(json).ConfigureAwait(false);
                if (!wrote) return;

                handle.WaitOne();
            }
        })
        { IsBackground = true }.Start();

        return ValueTask.CompletedTask;
    }
}
