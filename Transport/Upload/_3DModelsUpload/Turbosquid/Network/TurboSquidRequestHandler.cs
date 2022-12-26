using CefSharp;
using CefSharp.Handler;
using CefSharp.ResponseFilter;
using System.Text;
using Transport.Upload._3DModelsUpload.Turbosquid.Network.Authenticity;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Network;
internal class TurboSquidRequestHandler : RequestHandler
{
    protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling) =>
        _resourceRequestHandler;
    readonly IResourceRequestHandler _resourceRequestHandler = new TurboSquidResourceRequestHandler();
}

internal class TurboSquidResourceRequestHandler : ResourceRequestHandler
{
    protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
    {
        if (TurboSquidResponse.HasNetworkCredential(request))
            TurboSquidNetworkCredential._CapturedCefResponse.SetAsync(_Response).Wait();
        else if (TurboSquidResponse.HasCaptchaSolution(request))
            TurboSquidCaptchaVerifiedToken._CapturedCefResponse.SetAsync(_Response).Wait();
    }

    protected override IResponseFilter GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response) =>
        TurboSquidResponse.HasNetworkCredential(request) || TurboSquidResponse.HasCaptchaSolution(request) ? _InterceptResponse() : default!;

    IResponseFilter _InterceptResponse() => new StreamResponseFilter(_responseStream = new MemoryStream());

    string _Response => Encoding.UTF8.GetString(_responseStream!.ToArray());
    MemoryStream? _responseStream;
}

static class TurboSquidResponse
{
    internal static bool HasNetworkCredential(IRequest request) => HasSessionContext(request.Url);
    static bool HasSessionContext(string url) => new Uri(url).PathAndQuery == "/users/sign_in";

    internal static bool HasCaptchaSolution(IRequest resourceRequest) => HasCaptchaSolution(resourceRequest.Url);
    static bool HasCaptchaSolution(string resourceUrl) => resourceUrl.Contains("/reload");
}
