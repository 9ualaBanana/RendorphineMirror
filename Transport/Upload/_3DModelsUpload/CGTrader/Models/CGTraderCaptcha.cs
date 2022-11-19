using Transport.Upload._3DModelsUpload.CGTrader.Services;

namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

public sealed class CGTraderCaptcha
{
    readonly string _asBase64;

    public readonly string SiteKey;
    public readonly CGTraderCaptchaConfiguration Configuration;
    // make lateinit
    public string? VerfiedToken { get; internal set; }

    public string? UserGuess;

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

    internal async ValueTask<string> _SolveAsyncUsing(
        CGTraderCaptchaService captchaService,
        CancellationToken cancellationToken) => await captchaService._SolveCaptchaAsync(this, cancellationToken);

    #region Casts

    public static implicit operator string(CGTraderCaptcha captcha) => captcha._asBase64;

    #endregion
}
