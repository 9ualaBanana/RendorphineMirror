namespace Transport.Upload._3DModelsUpload.CGTrader.Network.Captcha;

internal static class CGTraderCaptchaSiteKey
{
    const char _ValueDelimiter = '"';
    static string _SiteKeyJsonKey = $"sitekey:{_ValueDelimiter}";

    internal static string _Parse(string htmlWithSessionCredentials)
    {
        if (htmlWithSessionCredentials.Contains(_SiteKeyJsonKey))
            return _ParseCoreFrom(htmlWithSessionCredentials);
        else
            throw new MissingFieldException("Returned document doesn't contain site key.");
    }

    static string _ParseCoreFrom(string htmlWithSessionCredentials)
    {
        int siteKeyStartIndex = htmlWithSessionCredentials.IndexOf(_SiteKeyJsonKey) + _SiteKeyJsonKey.Length;
        int siteKeyEndIndex = htmlWithSessionCredentials.IndexOf(_ValueDelimiter, siteKeyStartIndex);
        return htmlWithSessionCredentials[siteKeyStartIndex..siteKeyEndIndex];
    }
}
