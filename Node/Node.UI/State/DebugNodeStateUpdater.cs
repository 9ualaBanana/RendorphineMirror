namespace Node.UI.State;

public class DebugNodeStateUpdater
{
    public required NodeConnectionState NodeConnectionState { get; init; }

    public void Connect() => NodeConnectionState.IsConnectedToNode.Value = true;
    public void Disconnect() => NodeConnectionState.IsConnectedToNode.Value = false;
}
