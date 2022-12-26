using _3DProductsPublish.CGTrader.Api;
using _3DProductsPublish.CGTrader.Network;
using _3DProductsPublish.CGTrader.Network.Captcha;
using Common;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;
using NodeToUI;

namespace _3DProductsPublish.CGTrader.Api;

internal class CGTraderCaptchaApi : IBaseAddressProvider
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;

    string IBaseAddressProvider.BaseAddress => "https://service.mtcaptcha.com/mtcv1/api/";

    internal CGTraderCaptchaApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region Request

    internal async Task<CGTraderCaptcha> _RequestCaptchaAsync(string siteKey, CancellationToken cancellationToken)
    {
        try
        {
            var captcha = await _RequestCaptchaAsyncCore(siteKey, cancellationToken);
            _logger.Debug("CAPTCHA is received."); return captcha;
        }
        catch (Exception ex)
        {
            string errorMessage = "CAPTCHA request failed.";
            _logger.Error(errorMessage, ex); throw new Exception(errorMessage, ex);
        }
    }

    async Task<CGTraderCaptcha> _RequestCaptchaAsyncCore(string siteKey, CancellationToken cancellationToken)
    {
        var configuration = await _RequestCaptchaConfigurationsAsync(siteKey, cancellationToken);
        string imageAsBase64 = await _RequestCaptchaImageAsBase64Async(siteKey, configuration, cancellationToken);

        return CGTraderCaptcha._FromBase64String(imageAsBase64, siteKey, configuration);
    }

    async Task<CGTraderCaptchaConfiguration> _RequestCaptchaConfigurationsAsync(
        string siteKey,
        CancellationToken cancellationToken)
    {
        string requestUri = QueryHelpers.AddQueryString((this as IBaseAddressProvider).Endpoint("/getchallenge.json"),
            new Dictionary<string, string>()
            {
                { "sk", siteKey },
                { "bd", CGTraderUri.www },
                { "rt", CaptchaRequestArguments.rt },
                { "tsh", CaptchaRequestArguments.tsh },
                { "act", CaptchaRequestArguments.act },
                { "ss", CaptchaRequestArguments.ss },
                { "lf", CaptchaRequestArguments.lf },
                { "tl", CaptchaRequestArguments.tl },
                { "lg", CaptchaRequestArguments.lg },
                { "tp", CaptchaRequestArguments.tp }
            });

        var response = JObject.Parse(
            await _httpClient.GetStringAsync(requestUri, cancellationToken)
            )._EnsureSuccessStatusCode();

        var captchaConfiguration = response["result"]!["challenge"]!;
        var foldChallengeConfiguration = captchaConfiguration["foldChlg"]!;

        return new CGTraderCaptchaConfiguration(
            (string)captchaConfiguration["ct"]!,
            new CGTraderCaptchaFoldChallenge(
                (string)foldChallengeConfiguration["fseed"]!,
                (int)foldChallengeConfiguration["fslots"]!,
                (int)foldChallengeConfiguration["fdepth"]!)
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

        var response = JObject.Parse(
            await _httpClient.GetStringAsync(requestUri, cancellationToken)
            )._EnsureSuccessStatusCode();

        return (string)response["result"]!["img"]!["image64"]!;
    }

    #endregion

    #region Solving

    internal async Task<string> _SolveCaptchaAsync(CGTraderCaptcha captcha, CancellationToken cancellationToken)
    {
        try
        {
            string verifiedToken = await __SolveCaptchaAsync(captcha, cancellationToken);
            _logger.Debug("CAPTCHA was solved."); return verifiedToken;
        }
        catch (Exception ex)
        {
            string errorMessage = "CAPTCHA couldn't be solved.";
            _logger.Error(errorMessage, ex); throw new Exception(errorMessage, ex);
        }
    }

    async Task<string> __SolveCaptchaAsync(CGTraderCaptcha captcha, CancellationToken cancellationToken) =>
        await _SolveCaptchaAsyncCore(captcha, await _RequestCaptchaSolutionFromGuiAsync(captcha), cancellationToken);

    static async Task<string> _RequestCaptchaSolutionFromGuiAsync(string base64Image) =>
        (await NodeGui.RequestCaptchaInputAsync(base64Image))
        .ThrowIfError("Could not get captcha user input: {0}");

    /// <returns>Verified token.</returns>
    async Task<string> _SolveCaptchaAsyncCore(CGTraderCaptcha captcha, string solution, CancellationToken cancellationToken)
    {
        string requestUri = QueryHelpers.AddQueryString((this as IBaseAddressProvider).Endpoint("/solvechallenge.json"),
            new Dictionary<string, string>()
            {
                { "ct", captcha.Configuration.Token },
                { "sk", captcha.SiteKey },
                { "st", solution },
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
            ); responseWithVerifiedToken._EnsureSuccessStatusCode();
        var verifiedToken = responseWithVerifiedToken["result"]!["verifyResult"]!;

        if ((bool)verifiedToken["isVerified"]!)
            return (string)verifiedToken["verifiedToken"]!["vt"]!;
        else throw new UnauthorizedAccessException($"The {nameof(captcha)} didn't pass verification.");
    }

    #endregion
}

static class CaptchaJObjectExtensions
{
    internal static JObject _EnsureSuccessStatusCode(this JObject captchaServiceResponse)
    {
        if ((int)captchaServiceResponse["code"]! != 1200) throw new HttpRequestException(
            $"CGTrader CAPTCHA service response code does not indicate success: {captchaServiceResponse["code"]}\n" +
            $"{captchaServiceResponse["msgs"]}.");
        else return captchaServiceResponse;
    }
}
