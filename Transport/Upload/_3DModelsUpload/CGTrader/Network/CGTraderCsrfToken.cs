using System.Net.Http.Headers;

namespace Transport.Upload._3DModelsUpload.CGTrader.Network;

internal static class CGTraderCsrfToken
{
    internal static string _Parse(string html, CsrfTokenRequest tokenRequest) => tokenRequest switch
    {
        CsrfTokenRequest.Initial => _ParseInitial(html),
        CsrfTokenRequest.UploadInitializing => _ParseUploadInitializing(html),
        _ => throw new NotImplementedException("Unknown token kind.")
    };

    static string _ParseInitial(string htmlWithSessionCredentials)
    {
        string _CsrfTokenMetaContentKey = $"meta.content = '";
        int csrfTokenStartIndex = htmlWithSessionCredentials.IndexOf(_CsrfTokenMetaContentKey) + _CsrfTokenMetaContentKey.Length;
        int csrfTokenEndIndex = htmlWithSessionCredentials.IndexOf('\'', csrfTokenStartIndex);
        return htmlWithSessionCredentials[csrfTokenStartIndex..csrfTokenEndIndex];
    }

    static string _ParseUploadInitializing(string uploadInitializingCsrfToken)
    {
        const string CsrfTokenMeta = "<meta name=\"csrf-token\" content=\"";
        int csrfTokenStartIndex = uploadInitializingCsrfToken.IndexOf(CsrfTokenMeta) + CsrfTokenMeta.Length;
        int csrfTokenEndIndex = uploadInitializingCsrfToken.IndexOf('"', csrfTokenStartIndex);
        return uploadInitializingCsrfToken[csrfTokenStartIndex..csrfTokenEndIndex];
    }
}

internal enum CsrfTokenRequest
{
    Initial,
    UploadInitializing
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
