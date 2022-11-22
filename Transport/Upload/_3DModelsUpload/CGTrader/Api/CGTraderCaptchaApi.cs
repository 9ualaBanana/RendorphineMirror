using Common;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NodeToUI;
using Transport.Models;
using Transport.Upload._3DModelsUpload.CGTrader.Captcha;

namespace Transport.Upload._3DModelsUpload.CGTrader.Api;

internal class CGTraderCaptchaApi : IBaseAddressProvider
{
    readonly HttpClient _httpClient;

    string IBaseAddressProvider.BaseAddress => "https://service.mtcaptcha.com/mtcv1/api/";


    internal CGTraderCaptchaApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    internal async Task<CGTraderCaptcha> _RequestCaptchaAsync(
        string htmlWithSessionCredentials,
        CancellationToken cancellationToken)
    {
        string siteKey = _ParseSiteKeyFrom(htmlWithSessionCredentials);
        return await _RequestCaptchaAsyncCore(siteKey, cancellationToken);
    }

    #region SiteKey

    static string _ParseSiteKeyFrom(string htmlWithSessionCredentials)
    {
        var siteKeyRegion = new Range(3220, 4430);
        string siteKeyRegionContent = htmlWithSessionCredentials[siteKeyRegion];

        if (siteKeyRegionContent.Contains("sitekey"))
            return _ParseSiteKeyCoreFrom(siteKeyRegionContent);
        else
            throw new MissingFieldException("Returned document doesn't contain sitekey.");
    }

    static string _ParseSiteKeyCoreFrom(string siteKeyRegion) => siteKeyRegion
        .Split('{', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Last()[9..27];

    #endregion

    #region Captcha

    #region Request

    async Task<CGTraderCaptcha> _RequestCaptchaAsyncCore(string siteKey, CancellationToken cancellationToken)
    {
        var configuration = await _RequestCaptchaConfigurationsAsync(siteKey, cancellationToken);
        return CGTraderCaptcha._FromBase64String(
            await _RequestCaptchaImageAsBase64Async(siteKey, configuration, cancellationToken),
            siteKey, configuration);
    }

    async Task<CGTraderCaptchaConfiguration> _RequestCaptchaConfigurationsAsync(
        string siteKey,
        CancellationToken cancellationToken)
    {
        string requestUri = QueryHelpers.AddQueryString((this as IBaseAddressProvider).Endpoint($"/getchallenge.json"),
            new Dictionary<string, string>()
            {
                { "sk", siteKey },
                { "bd", CGTraderUri.WWW },
                { "rt", CaptchaRequestArguments.rt },
                { "tsh", CaptchaRequestArguments.tsh },
                { "act", CaptchaRequestArguments.act },
                { "ss", CaptchaRequestArguments.ss },
                { "lf", CaptchaRequestArguments.lf },
                { "tl", CaptchaRequestArguments.tl },
                { "lg", CaptchaRequestArguments.lg },
                { "tp", CaptchaRequestArguments.tp }
            });

        var responseWithCaptchaConfiguration = JObject.Parse(
            await _httpClient.GetStringAsync(requestUri, cancellationToken)
            );
        responseWithCaptchaConfiguration._EnsureSuccessStatusCode();
        var captchaConfiguration = responseWithCaptchaConfiguration._Result()["challenge"]!;
        var foldChallengeConfiguration = captchaConfiguration["foldChlg"]!;

        return new CGTraderCaptchaConfiguration(
            (string)captchaConfiguration["ct"]!,
            new CGTraderCaptchaFoldChallenge(
                (string)foldChallengeConfiguration["fseed"]!,
                (int)foldChallengeConfiguration["fslots"]!,
                (int)foldChallengeConfiguration["fdepth"]!
                )
            );
    }

    async Task<string> _RequestCaptchaImageAsBase64Async(
        string siteKey,
        CGTraderCaptchaConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var requestUri = QueryHelpers.AddQueryString((this as IBaseAddressProvider).Endpoint("/getimage.json"),
            new Dictionary<string, string>()
            {
                { "sk", siteKey },
                { "ct", configuration.Token },
                { "fa", configuration.FoldChallenge.Solve() },
                { "ss", CaptchaRequestArguments.ss }
            });

        var responseWithImage = JObject.Parse(await _httpClient.GetStringAsync(requestUri, cancellationToken));
        responseWithImage._EnsureSuccessStatusCode();

        return (string)responseWithImage._Result()["img"]!["image64"]!;
    }

    #endregion

    #region Solving

    internal async ValueTask<string> _SolveCaptchaAsync(CGTraderCaptcha captcha, CancellationToken cancellationToken)
    {
        if (captcha.VerfiedToken is null)
        {
            captcha.UserGuess = await _RequestCaptchaInputFromGuiAsync(captcha);
            captcha.VerfiedToken = await _VerifyCaptchaAsync(captcha, cancellationToken);
        }
        return captcha.VerfiedToken;
    }

    static async Task<string> _RequestCaptchaInputFromGuiAsync(string base64Image) =>
        (await NodeGui.RequestCaptchaInputAsync(base64Image))
        .ThrowIfError("Could not get captcha user input: {0}");

    /// <returns>Verified token.</returns>
    async Task<string> _VerifyCaptchaAsync(CGTraderCaptcha captcha, CancellationToken cancellationToken)
    {
        if (captcha.UserGuess is null) throw new ArgumentNullException(
            nameof(captcha.UserGuess),
            $"{nameof(captcha)} must contain {nameof(captcha.UserGuess)} to try to solve it."
            );

        string requestUri = QueryHelpers.AddQueryString((this as IBaseAddressProvider).Endpoint("/solvechallenge.json"),
            new Dictionary<string, string>()
            {
                { "ct", captcha.Configuration.Token },
                { "sk", captcha.SiteKey },
                { "st", captcha.UserGuess },
                { "lf", CaptchaRequestArguments.lf },
                { "bd", CaptchaRequestArguments.bd },
                { "rt", CaptchaRequestArguments.rt },
                { "tsh", CaptchaRequestArguments.tsh },
                { "fa", captcha.Configuration.FoldChallenge.Solve() },
                { "qh", CaptchaRequestArguments.qh },
                { "act", CaptchaRequestArguments.act },
                { "ss", CaptchaRequestArguments.ss },
                { "tl", CaptchaRequestArguments.tl },
                { "lg", CaptchaRequestArguments.lg },
                { "tp", CaptchaRequestArguments.tp },
                { "kt", CaptchaRequestArguments.kt },
                { "fs", captcha.Configuration.FoldChallenge.Seed }
            });

        var responseWithVerifiedToken = JObject.Parse(
            await _httpClient.GetStringAsync(requestUri, cancellationToken)
            );
        responseWithVerifiedToken._EnsureSuccessStatusCode();
        var verifiedToken = responseWithVerifiedToken._Result()["verifyResult"]!;

        if (!(bool)verifiedToken["isVerified"]!) throw new UnauthorizedAccessException(
            $"The {nameof(captcha)} didn't pass verification."
            );
        else return (string)verifiedToken["verifiedToken"]!["vt"]!;
    }

    #endregion

    #endregion
}

static class CaptchaJObjectExtensions
{
    internal static JToken _Result(this JObject captchaServiceResponse) => captchaServiceResponse["result"]!;
    internal static void _EnsureSuccessStatusCode(this JObject captchaServiceResponse)
    {
        if ((int)captchaServiceResponse["code"]! != 1200) throw new HttpRequestException(
            $"Response code does not indicate success: {captchaServiceResponse["code"]}\n" +
            $"{captchaServiceResponse["msgs"]}.");
    }
}
