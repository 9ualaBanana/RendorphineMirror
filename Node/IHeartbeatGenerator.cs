namespace Node;

internal interface IHeartbeatGenerator
{
    HttpRequestMessage Request { get; }
    EventHandler<HttpResponseMessage>? ResponseHandler { get; }
}
