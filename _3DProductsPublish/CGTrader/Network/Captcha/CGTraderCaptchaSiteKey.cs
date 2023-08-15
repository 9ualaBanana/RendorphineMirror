namespace _3DProductsPublish.CGTrader.Network.Captcha;

internal static class CGTraderCaptchaSiteKey
{
    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    internal static string _Parse(string document)
    {
        try
        {
            var siteKey = _ParseCoreFrom(document);
            _logger.Trace("Site key was successfully parsed from the document ({SiteKey}).", siteKey);
            return siteKey;
        }
        catch (Exception ex)
        {
            string errorMessage = "The document doesn't contain site key.";
            _logger.Error(ex, "{Message}\n{Document}", errorMessage, document);
            throw new MissingFieldException(errorMessage, ex);
        }
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
