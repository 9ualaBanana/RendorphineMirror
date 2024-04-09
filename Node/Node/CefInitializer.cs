using CefSharp.OffScreen;

namespace Node;

public static class CefInitializer
{
    static CefInitializer() => CefSharp.Cef.Initialize(new CefSettings { PersistSessionCookies = true, CachePath = Path.GetFullPath("cef_cache") });

    // Empty method to trigger the static ctor
    public static void Initialize() { }
}
