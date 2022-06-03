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
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _http = httpClient;
        _failedPingAttemptsLogLimit = failedPingAttemptsLogLimit;
        Log.Debug("{Service} is initialized with interval of {Interval} ms.", nameof(ServerPinger), interval);
    }

    internal async Task Start()
    {
        await PingServerAsync();
        _timer.Start();
        Log.Debug("{Service} is started.", nameof(ServerPinger));
    }

    async void OnTimerElapsed(object? s, ElapsedEventArgs e)
    {
        await PingServerAsync();
    }

    async Task PingServerAsync()
    {
        try
        {
            var queryString = $"nodeInfo={_nodeHardwareInfo.Name},{Init.Version}";
            var response = await _http.GetAsync($"{Settings.ServerUrl}/node/ping?{queryString}");
            response.EnsureSuccessStatusCode();
            Log.Debug("{Service} successfuly sent ping to {Server}", nameof(ServerPinger), Settings.ServerUrl);
        }
        catch (Exception ex)
        {
            _failedPingAttempts++;
            if (_failedPingAttempts <= _failedPingAttemptsLogLimit)
            {
                Log.Error(ex, "Node v.{Version} was not able to ping the server.", Init.Version);
            }
        }
    }

    public void Dispose()
    {
        _timer?.Close();
        Log.Debug("{Service} is disposed.", nameof(ServerPinger));
    }
}
