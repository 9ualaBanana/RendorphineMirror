using Hardware;
using Serilog;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Node;

internal class ServerPinger : IDisposable
{
    readonly HardwareInfo _nodeHardwareInfo;
    readonly Timer _timer;
    readonly HttpClient _http;
    readonly int _failedPingAttemptsLogLimit;
    int _failedPingAttempts;

    internal ServerPinger(HardwareInfo nodeHardwareInfo, TimeSpan timeSpan, HttpClient httpClient)
        : this(nodeHardwareInfo, timeSpan.TotalMilliseconds, httpClient)
    {
    }

    internal ServerPinger(
        HardwareInfo nodeHardwareInfo,
        double interval,
        HttpClient httpClient,
        int failedPingAttemptsLogLimit = 3)
    {
        _nodeHardwareInfo = nodeHardwareInfo;
        _timer = new Timer(interval);
        _timer.Elapsed += PingServer;
        _timer.AutoReset = true;
        _http = httpClient;
        _failedPingAttemptsLogLimit = failedPingAttemptsLogLimit;
        Log.Debug("{service} is initialized with interval of {interval} ms.", nameof(ServerPinger), interval);
    }

    internal void Start()
    {
        _timer.Start();
        Log.Debug("{service} is started.", nameof(ServerPinger));
    }

    async void PingServer(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var queryString = $"name={_nodeHardwareInfo.Name}&version={Init.Version}";
            var response = await _http.GetAsync($"{Settings.ServerUrl}/node/ping?{queryString}");
            response.EnsureSuccessStatusCode();
            Log.Debug("{service} successfuly sent ping to {server}", nameof(ServerPinger), Settings.ServerUrl);
        }
        catch (Exception ex)
        {
            _failedPingAttempts++;
            if (_failedPingAttempts <= _failedPingAttemptsLogLimit)
            {
                Log.Error(ex, "Node v.{version} was not able to ping the server.", Init.Version);
            }
        }
    }

    public void Dispose()
    {
        _timer?.Close();
        Log.Debug("{service} is disposed.", nameof(ServerPinger));
    }
}
