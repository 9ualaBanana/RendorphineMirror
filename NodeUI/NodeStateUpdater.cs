using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeUI;

public static class NodeStateUpdater
{
    readonly static Logger Logger = LogManager.GetCurrentClassLogger();

    public static readonly Bindable<string> NodeHost = new("127.0.0.1:5123");
    public static readonly Bindable<bool> IsConnectedToNode = new(false);

    static NodeStateUpdater()
    {
        try { NodeHost.Value = "127.0.0.1:" + ushort.Parse(File.ReadAllText(Path.Combine(Init.ConfigDirectory, "lport")).Trim()); }
        catch { }
    }

    public static void Start() => _Start().Consume();
    static async Task _Start()
    {
        var loadcache = Init.IsDebug;
        var cacheloaded = !loadcache;

        var cachefile = Path.Combine(Init.ConfigDirectory, "nodeinfocache");
        if (loadcache)
        {
            NodeGlobalState.Instance.AnyChanged.Subscribe(NodeGlobalState.Instance, _ =>
                File.WriteAllText(cachefile, JsonConvert.SerializeObject(NodeGlobalState.Instance, JsonSettings.Typed)));
        }

        var cancel = false;
        var consecutive = 0;
        while (true)
        {
            try
            {
                var stream = await LocalPipe.SendAsync($"http://{NodeHost.Value}/getstate").ConfigureAwait(false);

                var host = NodeHost.GetBoundCopy();
                using var _ = new FuncDispose(host.UnsubsbribeAll);
                host.Changed += () =>
                {
                    Logger.Info($"Node host was changed to {host.Value}; Restarting /getstate ...");
                    cancel = true;
                    stream.Close();
                };


                var reader = LocalPipe.CreateReader(stream);
                consecutive = 0;
                IsConnectedToNode.Value = true;

                while (true)
                {
                    var read = await reader.ReadAsync();
                    if (!read) break;
                    if (cancel) return;

                    var jtoken = await JToken.LoadAsync(reader);
                    Logger.Trace($"Node state updated: {string.Join(", ", (jtoken as JObject)?.Properties().Select(x => x.Name) ?? new[] { jtoken.ToString(Formatting.None) })}");

                    using var tokenreader = jtoken.CreateReader();
                    JsonSettings.TypedS.Populate(tokenreader, NodeGlobalState.Instance);
                }
            }
            catch (Exception ex)
            {
                if (cancel) return;

                IsConnectedToNode.Value = false;
                if (consecutive < 3) Logger.Error($"Could not read node state: {ex.Message}, reconnecting...");
                else if (consecutive == 3) Logger.Error($"Could not read node state after {consecutive} retries, disabling connection retry logging...");

                consecutive++;


                if (!cacheloaded)
                {
                    cacheloaded = true;

                    if (File.Exists(cachefile))
                    {
                        try { JsonConvert.PopulateObject(File.ReadAllText(cachefile), NodeGlobalState.Instance, JsonSettings.Typed); }
                        catch { }
                    }
                }
            }

            await Task.Delay(1_000).ConfigureAwait(false);
        }
    }
}
