namespace NodeUI;

public class Apis
{
    public const string RegistryUrl = NodeCommon.Apis.RegistryUrl;

    public static NodeCommon.Apis Default => new(Api.Default, NodeGlobalState.Instance.SessionId.ThrowIfNull("UI is not connected to node"));
}
