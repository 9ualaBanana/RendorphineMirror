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
        // `_keymaster_session` and `_turbosquid_artist_session` don't get updated but a new cookie with the same name is added instead.
        // If wrong `_keymaster_session` cookie is evaluated by the server, then request to https://auth.turbosquid.com/users/two_factor_authentication.user?locale=en fails.
        // If wrong `_turbosquid_artist_session` cookie is evaluated by the server, then request to https://www.squid.io/ fails.
        _destinationCookieContainer.Add(
            new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain )
            );
        return true;
    }

    public void Dispose() { }

    #endregion
}
