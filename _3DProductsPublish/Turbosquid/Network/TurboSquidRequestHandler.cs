using _3DProductsPublish.Turbosquid.Network.Authenticity;
using CefSharp;
using CefSharp.ResponseFilter;
using System.Net;
using System.Text;

namespace _3DProductsPublish.Turbosquid.Network;

internal class RequestHandler : CefSharp.Handler.RequestHandler
{
    internal static RequestHandler _ = new();
    protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
        => ResourceRequestHandler._;
}
internal class ResourceRequestHandler : CefSharp.Handler.ResourceRequestHandler
{
    internal static ResourceRequestHandler _ = new();
    protected override IResponseFilter? GetResourceResponseFilter(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response)
    {
        var uri = new Uri(request.Url);
        if (uri.PathAndQuery == "/users/sign_in" && response.StatusCode is (int)HttpStatusCode.OK
            || uri.PathAndQuery.Contains("/reload"))
            return Response.Read();
        else return null;
    }

    protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
    {
        var uri = new Uri(request.Url);
        if (uri.PathAndQuery == "/users/sign_in" && response.StatusCode is (int)HttpStatusCode.OK)
            TurboSquidNetworkCredential.Response.SetAsync(Response.Get()).Wait();
        else if (uri.PathAndQuery.Contains("/reload"))
            TurboSquidCaptchaVerifiedToken.Response.SetAsync(Response.Get()).Wait();
    }
}

internal static class Response
{
    internal static string Get() => Encoding.UTF8.GetString(_stream!.ToArray());
    internal static IResponseFilter Read() => new StreamResponseFilter(_stream = new MemoryStream());
    static MemoryStream? _stream;
}
