using CefSharp;
using CefSharp.OffScreen;
using System.Net;

namespace Transport;

internal class CookieCopyingVisitor : ICookieVisitor
{
    readonly CookieContainer _destinationCookieContainer;

    internal CookieCopyingVisitor(CookieContainer destinationCookieContainer)
    {
        _destinationCookieContainer = destinationCookieContainer;
    }

    #region ICookieVisitor

    public bool Visit(CefSharp.Cookie cookie, int count, int total, ref bool deleteCookie)
    {
        _destinationCookieContainer.Add(
            new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain )
            );
        return true; // Tells the framework to keep calling this method for the next Cef.Sharp.Cookie stored in the browser.
    }

    public void Dispose() { }

    #endregion
}

internal static class CookieHelperExtensions
{
    internal static void _DumpCookiesTo(this ChromiumWebBrowser browser, CookieContainer cookieContainer) =>
        browser.GetCookieManager().VisitAllCookies(new CookieCopyingVisitor(cookieContainer));
}
