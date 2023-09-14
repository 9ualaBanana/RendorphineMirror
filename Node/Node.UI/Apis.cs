namespace Node.UI;

public static class Apis
{
    public const string RegistryUrl = Node.Common.Apis.RegistryUrl;

#if DEBUG
    public static Node.Common.Apis Default => new(Api.Default, NodeGlobalState.Instance.AuthInfo.Value?.SessionId ?? "63fe288368974192c27a5388");
#else
    public static Node.Common.Apis Default => new(Api.Default, NodeGlobalState.Instance.SessionId.ThrowIfNull("UI is not connected to node"));
#endif
}
