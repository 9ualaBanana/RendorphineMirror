namespace Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

internal static class TurboSquidCsrfToken
{
    internal static string _Parse(string html)
    {
        const string CsrfTokenMeta = "<meta name=\"csrf-token\" content=\"";
        int csrfTokenStartIndex = html.IndexOf(CsrfTokenMeta) + CsrfTokenMeta.Length;
        int csrfTokenEndIndex = html.IndexOf('"', csrfTokenStartIndex);
        return html[csrfTokenStartIndex..csrfTokenEndIndex];
    }
}
