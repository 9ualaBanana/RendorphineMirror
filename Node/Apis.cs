namespace Node;

public static class Apis
{
    public const string RegistryUrl = NodeCommon.Apis.RegistryUrl;

    public static readonly NodeCommon.Apis Default = new(Api.Default, Settings.SessionId);
}
