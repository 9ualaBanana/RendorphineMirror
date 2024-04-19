using _3DProductsPublish.CGTrader.Captcha;
using Microsoft.AspNetCore.WebUtilities;

namespace _3DProductsPublish.CGTrader;

public partial class CGTrader
{
    internal class Captcha : HttpClient
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        readonly INodeGui _gui;

        public Captcha(INodeGui gui)
        {
            BaseAddress = new("https://service.mtcaptcha.com/mtcv1/api/");
            _gui = gui;
        }

        internal async Task<CGTraderCaptcha> RequestCaptchaAsync(string siteKey, CancellationToken cancellationToken)
        {
            try
            {
                var captcha = await RequestCaptchaAsyncCore();
                _logger.Debug("CAPTCHA is received."); return captcha;
            }
            catch (Exception ex)
            {
                string errorMessage = "CAPTCHA request failed.";
                _logger.Error(errorMessage, ex); throw new Exception(errorMessage, ex);
            }


            async Task<CGTraderCaptcha> RequestCaptchaAsyncCore()
            {
                var configuration = await RequestCaptchaConfigurationsAsync();
                string imageAsBase64 = await RequestCaptchaImageAsBase64Async();

                return CGTraderCaptcha._FromBase64String(imageAsBase64, siteKey, configuration);


                async Task<CGTraderCaptchaConfiguration> RequestCaptchaConfigurationsAsync()
                {
                    var requestUri = QueryHelpers.AddQueryString("getchallenge.json",
                        new Dictionary<string, string?>()
                        {
                            { "sk", siteKey },
                            { "bd", CGTraderCaptchaRequestArguments.bd },
                            { "rt", CGTraderCaptchaRequestArguments.rt },
                            { "tsh", CGTraderCaptchaRequestArguments.tsh },
                            { "act", CGTraderCaptchaRequestArguments.act },
                            { "ss", CGTraderCaptchaRequestArguments.ss },
                            { "lf", CGTraderCaptchaRequestArguments.lf },
                            { "tl", CGTraderCaptchaRequestArguments.tl },
                            { "lg", CGTraderCaptchaRequestArguments.lg },
                            { "tp", CGTraderCaptchaRequestArguments.tp }
                        });
                    var response = JObject.Parse(await GetStringAsync(requestUri, cancellationToken)).EnsureSuccessStatusCode();

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

                async Task<string> RequestCaptchaImageAsBase64Async()
                {
                    var requestUri = QueryHelpers.AddQueryString("getimage.json",
                        new Dictionary<string, string?>()
                        {
                            { "sk", siteKey },
                            { "ct", configuration.Token },
                            { "fa", configuration.FoldChallenge.Solve() },
                            { "ss", CGTraderCaptchaRequestArguments.ss }
                        });
                    var response = JObject.Parse(await GetStringAsync(requestUri, cancellationToken)).EnsureSuccessStatusCode();

                    return (string)response["result"]!["img"]!["image64"]!;
                }
            }
        }

        internal async Task<string> SolveCaptchaAsync(string siteKey, CancellationToken cancellationToken)
        {
            try
            {
                var captcha = await RequestCaptchaAsync(siteKey, cancellationToken);
                string verifiedToken = await SolveCaptchaAsyncCore(captcha, cancellationToken);
                _logger.Debug("CAPTCHA was solved."); return verifiedToken;
            }
            catch (Exception ex)
            {
                string errorMessage = "CAPTCHA couldn't be solved.";
                _logger.Error(errorMessage, ex); throw new Exception(errorMessage, ex);
            }


            /// <returns>Verified token.</returns>
            async Task<string> SolveCaptchaAsyncCore(CGTraderCaptcha captcha, CancellationToken cancellationToken)
            {
                string requestUri = QueryHelpers.AddQueryString("solvechallenge.json",
                    new Dictionary<string, string>()
                    {
                        { "ct", captcha.Configuration.Token },
                        { "sk", captcha.SiteKey },
                        { "st", await RequestCaptchaSolutionFromGuiAsync(captcha) },
                        { "lf", CGTraderCaptchaRequestArguments.lf },
                        { "bd", CGTraderCaptchaRequestArguments.bd },
                        { "rt", CGTraderCaptchaRequestArguments.rt },
                        { "tsh", CGTraderCaptchaRequestArguments.tsh },
                        { "fa", captcha.Configuration.FoldChallenge.Solve() },
                        { "qh", CGTraderCaptchaRequestArguments.qh },
                        { "act", CGTraderCaptchaRequestArguments.act },
                        { "ss", CGTraderCaptchaRequestArguments.ss },
                        { "tl", CGTraderCaptchaRequestArguments.tl },
                        { "lg", CGTraderCaptchaRequestArguments.lg },
                        { "tp", CGTraderCaptchaRequestArguments.tp },
                        { "kt", CGTraderCaptchaRequestArguments.kt },
                        { "fs", captcha.Configuration.FoldChallenge.Seed }
                    }!);

                var responseWithVerifiedToken = JObject.Parse(await GetStringAsync(requestUri, cancellationToken)).EnsureSuccessStatusCode();
                var verifiedToken = responseWithVerifiedToken["result"]!["verifyResult"]!;

                if ((bool)verifiedToken["isVerified"]!)
                    return (string)verifiedToken["verifiedToken"]!["vt"]!;
                else throw new UnauthorizedAccessException($"The {nameof(captcha)} didn't pass verification.");


                async Task<string> RequestCaptchaSolutionFromGuiAsync(string base64Image)
                    => (await _gui.RequestCaptchaInputAsync(base64Image, cancellationToken))
                    .ThrowIfError("Could not get captcha user input: {0}");
            }
        }
    }
}

static class CaptchaJObjectExtensions
{
    internal static JObject EnsureSuccessStatusCode(this JObject captchaServiceResponse)
    {
        if ((int)captchaServiceResponse["code"]! != 1200) throw new HttpRequestException(
            $"CGTrader CAPTCHA service response code does not indicate success: {captchaServiceResponse["code"]}\n" +
            $"{captchaServiceResponse["msgs"]}.");
        else return captchaServiceResponse;
    }
}
