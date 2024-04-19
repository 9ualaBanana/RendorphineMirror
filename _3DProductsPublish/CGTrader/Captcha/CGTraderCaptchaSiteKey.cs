namespace _3DProductsPublish.CGTrader.Captcha;

internal static class CGTraderCaptchaSiteKey
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    internal static string Parse(string document)
    {
        try
        {
            var siteKey = ParseCore();
            _logger.Trace("Site key was successfully parsed from the document ({SiteKey}).", siteKey);
            return siteKey;
        }
        catch (Exception ex)
        {
            string errorMessage = "The document doesn't contain site key.";
            _logger.Error(ex, "{Message}\n{Document}", errorMessage, document);
            throw new MissingFieldException(errorMessage, ex);
        }


        string ParseCore()
        {
            const char _ValueDelimiter = '"';
            string _SiteKeyJsonKey = $"sitekey:{_ValueDelimiter}";
            int siteKeyStartIndex = document.IndexOf(_SiteKeyJsonKey) + _SiteKeyJsonKey.Length;
            int siteKeyEndIndex = document.IndexOf(_ValueDelimiter, siteKeyStartIndex);
            return document[siteKeyStartIndex..siteKeyEndIndex];
        }
    }
}
