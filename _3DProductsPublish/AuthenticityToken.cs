﻿using _3DProductsPublish.Turbosquid.Upload;

namespace _3DProductsPublish;

internal static class AuthenticityToken
{
    internal const string Header = "X-CSRF-Token";

    internal static async Task<string> GetStringAndUpdateAuthenticityTokenAsync(this TurboSquid turbosquid, string? requestUri, CancellationToken cancellationToken)
    {
        try
        {
            var response = await turbosquid.GetStringAsync(requestUri, cancellationToken);
            turbosquid.Credential.Update(AuthenticityToken.ParseFromMetaTag(response));
            TurboSquid._logger.Trace("Authenticity token has been obtained.");
            return response;
        }
        catch (Exception ex)
        { throw new Exception("Authenticity token request failed.", ex); }
    }
    internal static string ParseFromJS(string document)
    {
        string CsrfTokenMetaContentKey = $"meta.content = '";
        int csrfTokenStartIndex = document.IndexOf(CsrfTokenMetaContentKey) + CsrfTokenMetaContentKey.Length;
        int csrfTokenEndIndex = document.IndexOf('\'', csrfTokenStartIndex);
        return document[csrfTokenStartIndex..csrfTokenEndIndex];
    }

    internal static string ParseFromMetaTag(string document)
    {
        const string CsrfTokenMeta = "<meta name=\"csrf-token\" content=\"";
        int csrfTokenStartIndex = document.IndexOf(CsrfTokenMeta) + CsrfTokenMeta.Length;
        int csrfTokenEndIndex = document.IndexOf('"', csrfTokenStartIndex);
        return document[csrfTokenStartIndex..csrfTokenEndIndex];
    }
}
