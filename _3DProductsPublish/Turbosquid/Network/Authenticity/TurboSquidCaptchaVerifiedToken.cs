namespace _3DProductsPublish.Turbosquid.Network.Authenticity;

internal static class TurboSquidCaptchaVerifiedToken
{
    internal static ForeignThreadValue<string> _CapturedCefResponse = new(false);

    internal static string _Parse(string html)
    {
        return html.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[3];
    }
}
