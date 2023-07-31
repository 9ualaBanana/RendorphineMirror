namespace Node.Heartbeat;

public class TelegramBotHeartbeat : Heartbeat
{
    protected override TimeSpan Interval { get; } = TimeSpan.FromMinutes(5);
    readonly Api Api;
    readonly PluginManager PluginManager;

    public TelegramBotHeartbeat(Api api, PluginManager pluginManager, ILogger<TelegramBotHeartbeat> logger) : base(logger)
    {
        Api = api;
        PluginManager = pluginManager;
    }

    protected override async Task Execute()
    {
        var content = await MachineInfo.AsJsonContentAsync(PluginManager);
        var post = await Api.Client.PostAsync($"{Settings.ServerUrl}/node/ping", content);
        post.EnsureSuccessStatusCode();
    }
}
