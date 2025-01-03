﻿using Timer = System.Timers.Timer;

namespace Common.Heartbeat;

public class Heartbeat : IDisposable
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpRequestMessage _request;
    readonly Timer _timer;
    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;
    readonly bool _verboseLogging;

    event EventHandler<HttpResponseMessage>? _responseReceived;
    internal event EventHandler<HttpResponseMessage>? ResponseReceived
    {
        add
        {
            _responseReceived += value;
            _logger.Trace("Response handler is attached to {Service} for {Url}", nameof(Heartbeat), _request.RequestUri);
        }
        remove
        {
            _responseReceived -= value;
            _logger.Trace("Response handler is detached from {Service} for {Url}", nameof(Heartbeat), _request.RequestUri);
        }
    }

    public Heartbeat(
        IHeartbeatGenerator heartbeatGenerator,
        TimeSpan interval,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
        : this(heartbeatGenerator, interval.TotalMilliseconds, httpClient, cancellationToken)
    {
    }
    
    public Heartbeat(
        IHeartbeatGenerator heartbeatGenerator,
        double interval,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
        : this(heartbeatGenerator.Request, interval, httpClient, cancellationToken)
    {
        ResponseReceived += heartbeatGenerator.ResponseHandler;
    }

    public Heartbeat(
        HttpRequestMessage request,
        TimeSpan interval,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
        : this(request, interval.TotalMilliseconds, httpClient, cancellationToken)
    {
    }

    public Heartbeat(
        HttpRequestMessage request,
        double interval,
        HttpClient httpClient,
        CancellationToken cancellationToken = default,
        bool logEachHeartbeatWithTraceLevel = true)
    {
        _request = request;
        _timer = ConfigureTimer(interval);
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
        _verboseLogging = logEachHeartbeatWithTraceLevel;

        _logger.Debug("{Service} for {Url} is initialized with {Interval} ms interval", nameof(Heartbeat), _request.RequestUri, interval);
    }

    Timer ConfigureTimer(double interval)
    {
        var timer = new Timer(interval);
        timer.Elapsed += async (_, _) => await TrySendHeartbeatAsync();
        timer.AutoReset = true;
        return timer;
    }

    public async Task StartAsync()
    {
        await TrySendHeartbeatAsync();
        _timer.Start();

        _logger.Info("{Service} for {Url} is started", nameof(Heartbeat), _request.RequestUri);
    }

    async Task TrySendHeartbeatAsync()
    {
        try
        {
            var response = await SendHeartbeatAsync(); if (_verboseLogging) _logger.Trace("Heartbeat was sent to {Url}", _request.RequestUri);
            _responseReceived?.Invoke(this, response);
        }
        catch (HttpRequestException ex) { if (_verboseLogging) _logger.Trace(ex, "Heartbeat couldn't be sent to {Url}", _request.RequestUri); }
        catch (Exception ex) { _logger.Error(ex, "Heartbeat couldn't be sent to {Url} due to unexpected error", _request.RequestUri); }
    }

    async Task<HttpResponseMessage> SendHeartbeatAsync()
    {
        var response = await _httpClient.SendAsync(new(_request.Method, _request.RequestUri) { Content = _request.Content }, _cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _timer?.Close();
        
        _logger.Debug("{Service} for {Url} is stopped", nameof(Heartbeat), _request.RequestUri);
    }
}
