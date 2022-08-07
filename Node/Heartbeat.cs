using Timer = System.Timers.Timer;

namespace Node;

internal class Heartbeat : IDisposable
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpRequestMessage _request;
    readonly Timer _timer;
    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;

    event EventHandler<HttpResponseMessage>? _responseReceived;
    internal event EventHandler<HttpResponseMessage>? ResponseReceived
    {
        add
        {
            _responseReceived += value;
            _logger.Debug("Response handler is attached to {Service} for {Url}", nameof(Heartbeat), _request.RequestUri);
        }
        remove
        {
            _responseReceived -= value;
            _logger.Debug("Response handler is detached from {Service} for {Url}", nameof(Heartbeat), _request.RequestUri);
        }
    }

    internal Heartbeat(
        IHeartbeatGenerator heartbeatGenerator,
        TimeSpan interval,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
        : this(heartbeatGenerator, interval.TotalMilliseconds, httpClient, cancellationToken)
    {
    }
    
    internal Heartbeat(
        IHeartbeatGenerator heartbeatGenerator,
        double interval,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
        : this(heartbeatGenerator.Request, interval, httpClient, cancellationToken)
    {
        ResponseReceived += heartbeatGenerator.ResponseHandler;
    }

    internal Heartbeat(
        HttpRequestMessage request,
        TimeSpan interval,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
        : this(request, interval.TotalMilliseconds, httpClient, cancellationToken)
    {
    }

    internal Heartbeat(
        HttpRequestMessage request,
        double interval,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        _request = request;
        _timer = ConfigureTimer(interval);
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;

        _logger.Debug("{Service} for {Url} is initialized with {Interval} ms interval", nameof(Heartbeat), _request.RequestUri, interval);
    }

    Timer ConfigureTimer(double interval)
    {
        var timer = new Timer(interval);
        timer.Elapsed += (_, _) => _ = TrySendHeartbeatAsync();
        timer.AutoReset = true;
        return timer;
    }

    internal async Task StartAsync()
    {
        await TrySendHeartbeatAsync();
        _timer.Start();

        _logger.Info("{Service} for {Url} is started", nameof(Heartbeat), _request.RequestUri);
    }

    async Task TrySendHeartbeatAsync()
    {
        try
        {
            var response = await SendHeartbeatAsync(); _logger.Debug("Heartbeat was sent to {Url}", _request.RequestUri);
            _responseReceived?.Invoke(this, response);
        }
        catch (HttpRequestException ex) { _logger.Error(ex, "Heartbeat couldn't be sent to {Url}", _request.RequestUri); }
        catch (Exception ex) { _logger.Error(ex, "Heartbeat couldn't be sent to {Url} due to unexpected error", _request.RequestUri); }
    }

    async Task<HttpResponseMessage> SendHeartbeatAsync()
    {
        var response = await _httpClient.SendAsync(new(_request.Method, _request.RequestUri) { Content = _request.Content }, _cancellationToken);
        await Api.GetJsonFromResponseIfSuccessfulAsync(response);
        return response;
    }

    public void Dispose()
    {
        _timer?.Close();

        _logger.Debug("{Service} for {Url} is stopped", nameof(Heartbeat), _request.RequestUri);
    }
}
