namespace Node.UI.State;

public class NodeConnectionState
{
    public required NodeGlobalState NodeGlobalState { get; init; }

    public Bindable<bool> IsConnectedToNode { get; } = new(false);
}
