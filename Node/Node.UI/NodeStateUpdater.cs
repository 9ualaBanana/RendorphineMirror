namespace Node.UI;

public class NodeStateUpdater
{
    public event Action<NodeStateUpdate>? OnReceive;
    public event Action<Exception>? OnException;

    public required ILogger<NodeStateUpdater> Logger { get; init; }

    public Bindable<string?> NodeHost { get; } = new(null);
    public Bindable<bool> IsConnectedToNode { get; } = new(false);

    public async Task ReceivingLoop()
    {
        while (NodeHost.Value is null)
        {
            try
            {
                var port = ushort.Parse(await File.ReadAllTextAsync(new DataDirs("renderfin").DataFile("lport")), CultureInfo.InvariantCulture);
                NodeHost.Value = $"127.0.0.1:{port}";

                break;
            }
            catch { }

            Thread.Sleep(100);
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
                    Logger.LogInformation($"Node host was changed to {host.Value}; Restarting /getstate ...");
                    cancel = true;
                    stream.Close();
                };


                var reader = LocalPipe.CreateReader(stream);
                consecutive = 0;

                while (true)
                {
                    var read = await reader.ReadAsync();
                    if (!read) break;
                    if (cancel) return;

                    var info = (await JToken.LoadAsync(reader)).ToObject<NodeStateUpdate>().ThrowIfNull();
                    OnReceive?.Invoke(info);

                    if (!IsConnectedToNode.Value)
                        IsConnectedToNode.Value = true;
                }
            }
            catch (Exception ex)
            {
                if (cancel) return;

                IsConnectedToNode.Value = false;
                if (consecutive < 3) Logger.LogError($"Could not read node state: {ex.Message}, reconnecting...");
                else if (consecutive == 3) Logger.LogError($"Could not read node state after {consecutive} retries, disabling connection retry logging...");

                consecutive++;
                OnException?.Invoke(ex);
            }

            await Task.Delay(1_000).ConfigureAwait(false);
        }
    }
}
