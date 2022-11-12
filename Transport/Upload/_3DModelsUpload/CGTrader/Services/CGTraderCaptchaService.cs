using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Drawing;
using Transport.Upload._3DModelsUpload.CGTrader.Models;

namespace Transport.Upload._3DModelsUpload.CGTrader.Services;

internal class CGTraderCaptchaService
{
    readonly HttpClient _httpClient;


    internal CGTraderCaptchaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    internal async Task<CGTraderCaptcha> _RequestCaptchaAsync(string htmlWithSessionCredentials, CancellationToken cancellationToken)
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
        .Split('{', 30, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Last()[9..27];

    #endregion

    #region Captcha

    #region Request

    async Task<CGTraderCaptcha> _RequestCaptchaAsyncCore(string siteKey, CancellationToken cancellationToken)
    {
        var (configurationToken, fSeed) = await _RequestCaptchaConfigurationsAsync(siteKey, cancellationToken);
        return CGTraderCaptcha._FromBase64String(
            await _RequestCaptchaImageAsBase64Async(siteKey, configurationToken, cancellationToken),
            siteKey, configurationToken, fSeed);
    }

    async Task<(string ConfigurationToken, string FSeed)> _RequestCaptchaConfigurationsAsync(
        string siteKey,
        CancellationToken cancellationToken)
    {
        string requestUri = QueryHelpers.AddQueryString(_Endpoint($"/getchallenge.json"), new Dictionary<string, string>()
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

        return (
            (string)captchaConfiguration["ct"]!,
            (string)captchaConfiguration["foldChlg"]!["fseed"]!
            );
    }

    async Task<string> _RequestCaptchaImageAsBase64Async(string siteKey, string configurationToken, CancellationToken cancellationToken)
    {
        var requestUri = QueryHelpers.AddQueryString(_Endpoint("/getimage.json"), new Dictionary<string, string>()
        {
            { "sk", siteKey },
            { "ct", configurationToken },
            { "fa", CaptchaRequestArguments.fa },
            { "ss", CaptchaRequestArguments.ss }
        });

        var responseWithImage = JObject.Parse(await _httpClient.GetStringAsync(requestUri, cancellationToken));
        responseWithImage._EnsureSuccessStatusCode();

        return (string)responseWithImage._Result()["img"]!["image64"]!;
    }

    #endregion

    #region Solve

    internal async Task<string> _SolveCaptchaAsync(CGTraderCaptcha captcha, CancellationToken cancellationToken)
    {
        string captchaFilePath = _SaveCaptchaImage(captcha);
        _ShowCaptchaImage(captchaFilePath);
        string? userCaptchaInput = Console.ReadLine();
        // TODO: Find out how `userCaptchaInput` should be passed to `_VerifyCaptchaAsync` method.
        await _VerifyCaptchaAsync(captcha, cancellationToken);
        return string.Empty;
    }

    string _SaveCaptchaImage(CGTraderCaptcha captcha)
    {
        using var imageStream = new MemoryStream(Convert.FromBase64String(captcha));
        string captchaFilePath = Path.ChangeExtension(
            Path.Combine(Path.GetTempPath(), $"captcha_{Guid.NewGuid()}"),
            ".jpg"
            );
        Image.FromStream(imageStream).Save(captchaFilePath);

        return captchaFilePath;
    }

    void _ShowCaptchaImage(string captchaFilePath) =>
        Process.Start(new ProcessStartInfo($"\"{captchaFilePath}\"") { CreateNoWindow = true });

    async Task<string> _VerifyCaptchaAsync(CGTraderCaptcha captcha, CancellationToken cancellationToken)
    {
        if (captcha.ConfigurationToken is null) throw new InvalidOperationException(
            $"The value of {nameof(captcha.ConfigurationToken)} can't be null when trying to solve {nameof(captcha)}."
            );


        string requestUri = QueryHelpers.AddQueryString("/solvechallenge.json", new Dictionary<string, string>()
        {
            { "ct", captcha.ConfigurationToken },
            { "sk", captcha.SiteKey },
            { "st", CaptchaRequestArguments.st },
            { "lf", CaptchaRequestArguments.lf },
            { "bd", CaptchaRequestArguments.bd },
            { "rt", CaptchaRequestArguments.rt },
            { "tsh", CaptchaRequestArguments.tsh },
            { "fa", CaptchaRequestArguments.fa },
            { "qh", CaptchaRequestArguments.qh },
            { "act", CaptchaRequestArguments.act },
            { "ss", CaptchaRequestArguments.ss },
            { "tl", CaptchaRequestArguments.tl },
            { "lg", CaptchaRequestArguments.lg },
            { "tp", CaptchaRequestArguments.tp },
            { "kt", CaptchaRequestArguments.kt },
            { "fs", captcha.FSeed }
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

    static string _Endpoint(string endpointWithoutDomain)
    {
        if (!endpointWithoutDomain.StartsWith('/'))
            endpointWithoutDomain = '/' + endpointWithoutDomain;

        return $"https://service.mtcaptcha.com/mtcv1/api{endpointWithoutDomain}";
    }
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
