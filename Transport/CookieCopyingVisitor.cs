using CefSharp;
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
            new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain)
            );
        return true;
    }

    public void Dispose() { }

    #endregion
}
