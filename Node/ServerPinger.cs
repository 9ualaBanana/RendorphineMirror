using System.Timers;
using Timer = System.Timers.Timer;

namespace Node;

internal class ServerPinger : IDisposable
{
    readonly string _serverUri;
    readonly Timer _timer;
    readonly HttpClient _http;
    readonly int _failedPingAttemptsLogLimit;
    int _failedPingAttempts;

    internal ServerPinger(string serverUri, TimeSpan timeSpan, HttpClient httpClient)
        : this(serverUri, timeSpan.TotalMilliseconds, httpClient)
    {
    }

    internal ServerPinger(
        string serverUri,
        double interval,
        HttpClient httpClient,
        int failedPingAttemptsLogLimit = 3)
    {
        _serverUri = serverUri;
        _timer = new Timer(interval);
        _timer.Elapsed += (_, _) => _ = PingServerAsync();
        _timer.AutoReset = true;
        _http = httpClient;
        _failedPingAttemptsLogLimit = failedPingAttemptsLogLimit;
        Log.Debug("{Service} is initialized with interval of {Interval} ms.", nameof(ServerPinger), interval);
    }

    internal async Task StartAsync()
    {
        await PingServerAsync();
        _timer.Start();
        Log.Debug("{Service} is started.", nameof(ServerPinger));
    }

    async Task PingServerAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{_serverUri}?{(await MachineInfo.AsDTOAsync()).ToQueryString()}");
            response.EnsureSuccessStatusCode();
            Log.Debug("{Service} successfuly sent ping to {Server}", nameof(ServerPinger), Settings.ServerUrl);
        }
        catch (Exception ex)
        {
            _failedPingAttempts++;
            if (_failedPingAttempts <= _failedPingAttemptsLogLimit)
            {
                Log.Error(ex, "{Node} was not able to ping the server.", await MachineInfo.GetBriefInfoAsync());
            }
        }
    }

    public void Dispose()
    {
        _timer?.Close();
        Log.Debug("{Service} is disposed.", nameof(ServerPinger));
    }
}
