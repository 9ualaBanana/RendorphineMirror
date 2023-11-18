namespace Node.UI;

public record UIApis(Api Api, bool LogErrors = true) : Apis(Api, LogErrors)
{
    public required NodeGlobalState NodeGlobalState { get; init; }

    public override string SessionId => (NodeGlobalState.AuthInfo.Value?.SessionId).ThrowIfNull("UI is not connected to node");

    public UIApis(Api api) : this(api, true) { }
}

