using Transport.Upload._3DModelsUpload.CGTrader.Services;

namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

public sealed class Captcha
{
    readonly string _asBase64;

    public readonly string SiteKey;
    public readonly CaptchaConfiguration Configuration;
    // make lateinit
    public string? VerfiedToken { get; internal set; }

    public string? UserGuess;

    #region Initialization

    internal static Captcha _FromBase64String(string? captcha, string siteKey, CaptchaConfiguration configuration) =>
        new(captcha ?? string.Empty, siteKey, configuration);

    Captcha(string captchaAsBase64, string siteKey, CaptchaConfiguration configuration)
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

    public static implicit operator string(Captcha captcha) => captcha._asBase64;

    #endregion
}
