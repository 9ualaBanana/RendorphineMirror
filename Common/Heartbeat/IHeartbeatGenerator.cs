namespace Common.Heartbeat;

public interface IHeartbeatGenerator
{
    HttpRequestMessage Request { get; }
    EventHandler<HttpResponseMessage>? ResponseHandler { get; }
}
