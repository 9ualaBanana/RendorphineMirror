namespace Node.Heartbeat;

public class TelegramBotHeartbeat : Heartbeat
{
    protected override TimeSpan Interval { get; } = TimeSpan.FromMinutes(5);

    public required Api Api { get; init; }
    public required PluginManager PluginManager { get; init; }
    public required MachineInfoProvider MachineInfoProvider { get; init; }

    public TelegramBotHeartbeat(ILogger<TelegramBotHeartbeat> logger) : base(logger) { }

    protected override async Task Execute()
    {
        var content = new StringContent(JsonConvert.SerializeObject(await MachineInfoProvider.GetAsync()));
        var post = await Api.Client.PostAsync($"{Settings.ServerUrl}/node/ping", content);
        post.EnsureSuccessStatusCode();
    }
}
