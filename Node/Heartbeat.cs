using Timer = System.Timers.Timer;

namespace Node;

internal class Heartbeat : IDisposable
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly string _url;
    readonly Timer _timer;
    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;
    readonly HttpContent? _data;

    event EventHandler<HttpResponseMessage>? _responseReceived;
    internal event EventHandler<HttpResponseMessage>? ResponseReceived
    {
        add
        {
            _responseReceived += value;
            _logger.Debug("Response handler is attached to {Service} for {Url}", nameof(Heartbeat), _url);
        }
        remove
        {
            _responseReceived -= value;
            _logger.Debug("Response handler is detached from {Service} for {Url}", nameof(Heartbeat), _url);
        }
    }

    internal Heartbeat(
        string url,
        TimeSpan timeSpan,
        HttpClient httpClient,
        HttpContent? data = null,
        CancellationToken cancellationToken = default)
        : this(url, timeSpan.TotalMilliseconds, httpClient, data, cancellationToken)
    {
    }

    internal Heartbeat(
        string url,
        double interval,
        HttpClient httpClient,
        HttpContent? data = null,
        CancellationToken cancellationToken = default)
    {
        _url = url;
        _timer = new Timer(interval);
        _timer.Elapsed += (_, _) => _ = TrySendHeartbeatAsync();
        _timer.AutoReset = true;
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
        _data = data;

        _logger.Debug("{Service} for {Url} is initialized with {Interval} ms interval", nameof(Heartbeat), _url, interval);
    }

    internal async Task StartAsync()
    {
        await TrySendHeartbeatAsync();
        _timer.Start();

        _logger.Info("{Service} for {Url} is started", nameof(Heartbeat), _url);
    }

    async Task TrySendHeartbeatAsync()
    {
        try
        {
            var response = await SendHeartbeatAsync(); _logger.Debug("Heartbeat was sent to {Url}", _url);
            _responseReceived?.Invoke(this, response);
        }
        catch (HttpRequestException ex) { _logger.Error(ex, "Heartbeat couldn't be sent to {Url}", _url); }
        catch (Exception ex) { _logger.Error(ex, "Heartbeat couldn't be sent to {Url} due to unexpected error", _url); }
    }
    async Task<HttpResponseMessage> SendHeartbeatAsync()
    {
        var response = await _httpClient.PostAsync(_url, _data);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public void Dispose()
    {
        _timer?.Close();

        _logger.Debug("{Service} for {Url} is stopped", nameof(Heartbeat), _url);
    }
}
