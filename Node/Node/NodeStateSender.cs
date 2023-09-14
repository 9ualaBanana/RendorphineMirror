namespace Node;

public class NodeStateSender
{
    public required NodeGlobalState State { get; init; }
    public required ILogger<NodeStateSender> Logger { get; init; }

    readonly List<Instance> Instances = new();

    public async Task SendingLoop(Stream stream)
    {
        var instance = new Instance(stream) { State = State };
        Instances.Add(instance);
        using var _ = new FuncDispose(instance.Dispose);

        await instance.SendingLoop();
    }

    public async Task TrySendAsync(NodeStateUpdate update)
    {
        foreach (var instance in Instances.ToArray())
        {
            try { await instance.SendAsync(update); }
            catch (Exception ex) { Logger.LogError(ex, null); }
        }
    }


    public class Instance : IDisposable
    {
        public required NodeGlobalState State { get; init; }

        readonly SemaphoreSlim WriteLock = new(1);
        readonly LocalPipe.Writer Writer;

        public Instance(Stream stream) : this(new LocalPipe.Writer(stream)) { }
        public Instance(LocalPipe.Writer writer) => Writer = writer;

        public async Task SendAsync(NodeStateUpdate update)
        {
            await WriteLock.WaitAsync();
            using var _ = new FuncDispose(() => WriteLock.Release());

            await Writer.WriteAsync(update).ConfigureAwait(false);
        }

        public async Task SendingLoop()
        {
            var exittask = new TaskCompletionSource();

            NodeGlobalState.Instance.AnyChanged.Subscribe(this, v => send(v).Consume());
            await send(null);
            await exittask.Task.ConfigureAwait(false);


            async Task send(string? changed)
            {
                try
                {
                    JObject json;
                    if (changed is null) json = JObject.FromObject(State, JsonSettings.TypedS);
                    else json = new JObject() { [changed] = JToken.FromObject(typeof(NodeGlobalState).GetField(changed)!.GetValue(State)!, JsonSettings.TypedS), };

                    await SendAsync(new NodeStateUpdate(NodeStateUpdate.UpdateType.State, json)).ConfigureAwait(false);
                }
                catch (Exception ex) { exittask.SetException(ex); }
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            WriteLock.Dispose();
            Writer.Dispose();
        }
    }
}
