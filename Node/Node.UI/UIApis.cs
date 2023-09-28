namespace Node.UI;

public record UIApis(Api Api, bool LogErrors = true) : Node.Common.Apis(Api, LogErrors)
{
    public required NodeGlobalState NodeGlobalState { get; init; }

    public override string SessionId => (NodeGlobalState.Instance.AuthInfo.Value?.SessionId).ThrowIfNull("UI is not connected to node");

    public UIApis(Api api) : this(api, true) { }
}

