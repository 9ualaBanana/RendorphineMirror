namespace Common;

public static class HttpHelper
{
    public static string AddSchemeIfNeeded(string url, string scheme)
    {
        if (!scheme.EndsWith("://", StringComparison.Ordinal)) scheme += "://";

        if (url.StartsWith(scheme, StringComparison.Ordinal)) return url;
        return scheme + url;
    }
}
