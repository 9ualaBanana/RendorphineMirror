using Transport.Upload._3DModelsUpload.CGTrader.Services;

namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

public class CGTraderCaptcha
{
    readonly string _asBase64;

    public readonly string SiteKey;
    public readonly string ConfigurationToken;
    public readonly string FSeed;
    // make lateinit
    public string? VerfiedToken { get; internal set; }

    #region Initialization

    internal static CGTraderCaptcha _FromBase64String(string? captcha, string siteKey, string configurationToken, string fSeed) =>
        new(captcha ?? string.Empty, siteKey, configurationToken, fSeed);

    CGTraderCaptcha(string captchaAsBase64, string siteKey, string configurationToken, string fSeed)
    {
        _asBase64 = captchaAsBase64;
        SiteKey = siteKey;
        ConfigurationToken = configurationToken;
        FSeed = fSeed;
    }

    #endregion

    internal async Task<string> _SolveAsyncUsing(CGTraderCaptchaService captchaService, CancellationToken cancellationToken) =>
        await captchaService._SolveCaptchaAsync(this, cancellationToken);

    #region Casts

    public static implicit operator string(CGTraderCaptcha captcha) => captcha._asBase64;

    #endregion
}
