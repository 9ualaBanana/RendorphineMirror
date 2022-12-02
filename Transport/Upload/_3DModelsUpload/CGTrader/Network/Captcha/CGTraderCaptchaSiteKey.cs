namespace Transport.Upload._3DModelsUpload.CGTrader.Network.Captcha;

internal static class CGTraderCaptchaSiteKey
{
    internal static string _Parse(string html)
    {
        try { return _ParseCoreFrom(html); }
        catch (Exception ex) { throw new MissingFieldException("Returned document doesn't contain site key.", ex); }
    }

    static string _ParseCoreFrom(string html)
    {
        const char _ValueDelimiter = '"';
        string _SiteKeyJsonKey = $"sitekey:{_ValueDelimiter}";
        int siteKeyStartIndex = html.IndexOf(_SiteKeyJsonKey) + _SiteKeyJsonKey.Length;
        int siteKeyEndIndex = html.IndexOf(_ValueDelimiter, siteKeyStartIndex);
        return html[siteKeyStartIndex..siteKeyEndIndex];
    }
}
