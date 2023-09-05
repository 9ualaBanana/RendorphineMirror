namespace NodeToUI;

public static class Software
{
    static ValueTask<OperationResult<ImmutableDictionary<string, SoftwareDefinition>>> LoadSoftware() =>
        Api.Default.ApiGet<ImmutableDictionary<string, SoftwareDefinition>>($"{Apis.RegistryUrl}/getsoft", "value", "Getting registry software")
            .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());
    static ValueTask<OperationResult<ImmutableDictionary<string, SoftwareStats>>> LoadSoftwareStats() =>
        Api.Default.ApiGet<ImmutableDictionary<string, SoftwareStats>>($"{Api.TaskManagerEndpoint}/getsoftwarestats", "stats", "Getting software stats")
            .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());


    public static void StartUpdating(IReadOnlyBindable<bool>? skipupdate, ILogger logger, CancellationToken token)
    {
        new Thread(async () =>
        {
            var interval = TimeSpan.FromMinutes(5);

            while (true)
            {
                if (token.IsCancellationRequested) return;

                if (skipupdate?.Value != true)
                    (await OperationResult.WrapException(update)).LogIfError(logger);

                await Task.Delay(interval);


                async ValueTask update()
                {
                    var soft = LoadSoftware();
                    var stats = LoadSoftwareStats();

                    NodeGlobalState.Instance.Software.Value = await soft.ThrowIfError();
                    NodeGlobalState.Instance.SoftwareStats.Value = await stats.ThrowIfError();
                }
            }
        })
        { IsBackground = true }.Start();
    }
}
