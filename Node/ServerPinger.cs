using Serilog;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Node;

internal class ServerPinger
{
    readonly Timer _timer;
    readonly HttpClient _http;
    readonly int _failedPingAttemptsLogLimit;
    int _failedPingAttempts;

    internal ServerPinger(TimeSpan timeSpan, HttpClient httpClient)
        : this(timeSpan.TotalMilliseconds, httpClient)
    {
    }

    internal ServerPinger(double interval, HttpClient httpClient, int failedPingAttemptsLogLimit = 3)
    {
        _timer = new Timer(interval);
        _timer.Elapsed += PingServer;
        _timer.AutoReset = true;
        _http = httpClient;
        _failedPingAttemptsLogLimit = failedPingAttemptsLogLimit;
    }

    internal void Start()
    {
        _timer.Start();
    }

    async void PingServer(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var response = await _http.GetAsync($"{Settings.ServerUrl}/node_ping");
            response.EnsureSuccessStatusCode();
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
}
