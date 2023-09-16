namespace NodeToUI;

public static class Updaters
{
    [AutoRegisteredService(true)]
    public abstract class UpdaterBase<T>
    {
        protected abstract TimeSpan Interval { get; }
        protected ILogger Logger { get; }

        protected UpdaterBase(ILogger<UpdaterBase<T>> logger) => Logger = logger;

        public void Start(IReadOnlyBindable<bool>? skipupdate, Bindable<T> bindable, CancellationToken token)
        {
            new Thread(async () =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested) return;

                    if (skipupdate?.Value != true)
                        await OperationResult.WrapException(async () => bindable.Value = await GetValue()).LogIfError(Logger);

                    await Task.Delay(Interval);
                }
            })
            { IsBackground = true }.Start();
        }

        protected abstract Task<T> GetValue();
    }

    public class BalanceUpdater : UpdaterBase<UserBalance>
    {
        protected override TimeSpan Interval => TimeSpan.FromMinutes(5);
        public required Apis Api { get; init; }

        public BalanceUpdater(ILogger<BalanceUpdater> logger) : base(logger) { }

        public ValueTask<OperationResult<UserBalance>> GetMyBalanceAsync() =>
            Api.Api.ApiGet<UserBalance>($"{(Common.Api.ServerUri)}/rphtasklauncher/getmybalance", null, "Getting balance", Api.AddSessionId());

        protected override async Task<UserBalance> GetValue() =>
            await GetMyBalanceAsync().ThrowIfError().ConfigureAwait(false);
    }

    public class SoftwareUpdater : UpdaterBase<ImmutableDictionary<string, SoftwareDefinition>>
    {
        protected override TimeSpan Interval => TimeSpan.FromMinutes(5);
        public required Api Api { get; init; }

        public SoftwareUpdater(ILogger<SoftwareUpdater> logger) : base(logger) { }

        public ValueTask<OperationResult<ImmutableDictionary<string, SoftwareDefinition>>> GetMyBalanceAsync() =>
            Api.ApiGet<ImmutableDictionary<string, SoftwareDefinition>>($"{Apis.RegistryUrl}/getsoft", "value", "Getting registry software");

        protected override async Task<ImmutableDictionary<string, SoftwareDefinition>> GetValue() =>
            await GetMyBalanceAsync().ThrowIfError().ConfigureAwait(false);
    }

    public class SoftwareStatsUpdater : UpdaterBase<ImmutableDictionary<string, SoftwareStats>>
    {
        protected override TimeSpan Interval => TimeSpan.FromMinutes(5);
        public required Api Api { get; init; }

        public SoftwareStatsUpdater(ILogger<SoftwareStatsUpdater> logger) : base(logger) { }

        public ValueTask<OperationResult<ImmutableDictionary<string, SoftwareStats>>> GetMyBalanceAsync() =>
            Api.ApiGet<ImmutableDictionary<string, SoftwareStats>>($"{Api.TaskManagerEndpoint}/getsoftwarestats", "stats", "Getting software stats");

        protected override async Task<ImmutableDictionary<string, SoftwareStats>> GetValue() =>
            await GetMyBalanceAsync().ThrowIfError().ConfigureAwait(false);
    }
}
