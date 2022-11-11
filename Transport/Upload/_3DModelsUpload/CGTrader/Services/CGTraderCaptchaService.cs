using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
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

    async Task<CGTraderCaptcha> _RequestCaptchaAsyncCore(string siteKey, CancellationToken cancellationToken)
    {
        string configurationToken = await _RequestCaptchaConfigurationTokenAsync(siteKey, cancellationToken);
        return CGTraderCaptcha._FromBase64String(
            await _RequestCaptchaImageAsBase64Async(siteKey, configurationToken, cancellationToken)
            );
    }

    async Task<string> _RequestCaptchaConfigurationTokenAsync(string siteKey, CancellationToken cancellationToken)
    {
        string requestUri = QueryHelpers.AddQueryString(Endpoint($"/getchallenge.json"), new Dictionary<string, string>()
        {
            { "sk", siteKey },
            { "bd", CGTraderUri.WWW },
            { "rt", CGTraderRequestArguments.rt },
            { "tsh", "TH[31e658709b71e41628a6dd03cbf73e1f]" },
            { "act", "$" },
            { "ss", CGTraderRequestArguments.ss },
            { "lf", "1" },
            { "tl", "$" },
            { "lg", "en" },
            { "tp", "s" }
        });

        string captchaConfiguration = await _httpClient.GetStringAsync(requestUri, cancellationToken);

        return (string)JObject.Parse(captchaConfiguration)["result"]!["challenge"]!["ct"]!;
    }

    async Task<string> _RequestCaptchaImageAsBase64Async(string siteKey, string configurationToken, CancellationToken cancellationToken)
    {
        var requestUri = QueryHelpers.AddQueryString(Endpoint("/getimage.json"), new Dictionary<string, string>()
        {
            { "sk", siteKey },
            { "ct", configurationToken },
            { "fa", "$" },
            { "ss", CGTraderRequestArguments.ss }
        });

        var captchaImage = await _httpClient.GetStringAsync(requestUri, cancellationToken);

        return (string)JObject.Parse(captchaImage)["result"]!["img"]!["image64"]!;
    }

    string _ParseSiteKeyFrom(string htmlWithSessionCredentials)
    {
        var captchaRegion = new Range(3220, 4430);
        string captchaRegionContent = htmlWithSessionCredentials[captchaRegion];

        if (captchaRegionContent.Contains("sitekey"))
            return CGTraderCaptcha._FromBase64String(_ParseAsBase64From(captchaRegionContent));
        else
            throw new MissingFieldException("Returned document doesn't contain CAPTCHA.");
    }

    static string _ParseAsBase64From(string captchaRegion) => captchaRegion
        .ReplaceLineEndings(string.Empty)
        .Split('{', 30, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Last()[9..27];

    static string Endpoint(string endpointWithoutDomain)
    {
        if (!endpointWithoutDomain.StartsWith('/'))
            endpointWithoutDomain = '/' + endpointWithoutDomain;

        return $"https://service.mtcaptcha.com/mtcv1/api{endpointWithoutDomain}";
    }
}
