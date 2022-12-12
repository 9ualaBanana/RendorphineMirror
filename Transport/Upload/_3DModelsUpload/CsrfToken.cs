using System.Net.Http.Headers;

namespace Transport.Upload._3DModelsUpload;

internal static class CsrfToken
{
    internal static string _ParseFromJS(string document)
    {
        string CsrfTokenMetaContentKey = $"meta.content = '";
        int csrfTokenStartIndex = document.IndexOf(CsrfTokenMetaContentKey) + CsrfTokenMetaContentKey.Length;
        int csrfTokenEndIndex = document.IndexOf('\'', csrfTokenStartIndex);
        return document[csrfTokenStartIndex..csrfTokenEndIndex];
    }

    internal static string _ParseFromMetaTag(string document)
    {
        const string CsrfTokenMeta = "<meta name=\"csrf-token\" content=\"";
        int csrfTokenStartIndex = document.IndexOf(CsrfTokenMeta) + CsrfTokenMeta.Length;
        int csrfTokenEndIndex = document.IndexOf('"', csrfTokenStartIndex);
        return document[csrfTokenStartIndex..csrfTokenEndIndex];
    }
}

internal static class CsrfTokenExtensions
{
    internal static void _AddOrReplaceCsrfToken(this HttpRequestHeaders headers, string csrfToken)
    {
        const string Header = "X-CSRF-Token";

        headers.Remove(Header);
        headers.Add(Header, csrfToken);
    }
}
