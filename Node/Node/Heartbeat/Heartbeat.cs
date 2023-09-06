namespace Node.Heartbeat;

public abstract class Heartbeat
{
    protected abstract TimeSpan Interval { get; }
    protected readonly ILogger Logger;

    protected Heartbeat(ILogger logger) => Logger = logger;

    public void Start()
    {
        new Thread(async () =>
        {
            while (true)
            {
                try { await Execute(); }
                catch (Exception ex) { Logger.Error(ex); }

                await Task.Delay(Interval);
            }
        })
        { IsBackground = true, Name = GetType().Name }.Start();
    }

    public async Task RunOnce() => await Execute();
    protected abstract Task Execute();
}