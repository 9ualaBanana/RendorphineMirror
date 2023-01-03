namespace Common.Heartbeat;

public interface IHeartbeatGenerator
{
    HttpRequestMessage Request { get; }
    HttpContent? Content { get; }
    EventHandler<HttpResponseMessage>? ResponseHandler { get; }
}
