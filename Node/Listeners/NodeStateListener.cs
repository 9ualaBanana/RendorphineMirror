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
            var bindable = GlobalState.BState.GetBoundCopy();
            bindable.SubscribeChanged((oldv, newv) => handle.Set(), true);


            // when state changed, send it
            // if new state is not Idle, continue sending it every second until it's Idle

            var writer = new LocalPipe.Writer(context.Response.OutputStream);
            while (true)
            {
                handle.WaitOne();

                var wrote = await writer.WriteAsync(GlobalState.State).ConfigureAwait(false);
                if (!wrote) return;

                if (GlobalState.State is IdleNodeState) continue;

                while (true)
                {
                    wrote = await writer.WriteAsync(GlobalState.State).ConfigureAwait(false);
                    if (!wrote) return;

                    await Task.Delay(1_000).ConfigureAwait(false);
                    if (GlobalState.State is IdleNodeState) break;
                }
            }
        })
        { IsBackground = true }.Start();

        return ValueTask.CompletedTask;
    }
}
