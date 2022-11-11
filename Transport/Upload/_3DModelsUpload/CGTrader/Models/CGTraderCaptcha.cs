namespace Transport.Upload._3DModelsUpload.CGTrader.Models;

public class CGTraderCaptcha
{
    readonly string _asBase64;

    #region Initialization

    internal static CGTraderCaptcha _FromBase64String(string? captcha) => new(captcha ?? string.Empty);

    CGTraderCaptcha(string captchaAsBase64)
    {
        _asBase64 = captchaAsBase64;
    }

    #endregion

    internal string _Solve()
    {
        return string.Empty;
    }

    #region Casts

    public static implicit operator string(CGTraderCaptcha captcha) => captcha._asBase64;

    #endregion
}
