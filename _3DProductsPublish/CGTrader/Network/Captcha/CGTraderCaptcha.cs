namespace _3DProductsPublish.CGTrader.Network.Captcha;

public sealed class CGTraderCaptcha
{
    readonly string _asBase64;

    public readonly string SiteKey;
    public readonly CGTraderCaptchaConfiguration Configuration;

    #region Initialization

    internal static CGTraderCaptcha _FromBase64String(string? captcha, string siteKey, CGTraderCaptchaConfiguration configuration) =>
        new(captcha ?? string.Empty, siteKey, configuration);

    CGTraderCaptcha(string captchaAsBase64, string siteKey, CGTraderCaptchaConfiguration configuration)
    {
        _asBase64 = captchaAsBase64;
        SiteKey = siteKey;
        Configuration = configuration;
    }

    #endregion

    #region Casts

    public static implicit operator string(CGTraderCaptcha captcha) => captcha._asBase64;

    #endregion
}
