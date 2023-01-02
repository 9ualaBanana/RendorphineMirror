using CefSharp;
using CefSharp.OffScreen;

namespace NodeToUI;

public static class CefInitializer
{
    static bool Initialized = false;
    static readonly SemaphoreSlim Semaphore = new(0, 1);

    public static void StaticCtor() { }
    static CefInitializer()
    {
        new Thread(() =>
        {
            while (true)
            {
                try
                {
                    Semaphore.Wait();
                    if (Initialized)
                    {
                        Cef.Initialize(new CefSettings()
                        {
                            // NoSandbox = true,
                            MultiThreadedMessageLoop = true,
                            WindowlessRenderingEnabled = true,
                            LogFile = Path.Combine(Path.GetTempPath(), "renderfin", "ceflog.log"),
                            UserDataPath = "/temp/cef/data",
                        });
                    }
                    else Cef.Shutdown();
                }
                catch (Exception ex) { LogManager.GetCurrentClassLogger().Error(ex); }
            }
        })
        { IsBackground = true }.Start();
    }

    public static void Initialize()
    {
        Initialized = true;
        Semaphore.Release();
    }
    public static void Shutdown()
    {
        Initialized = false;
        Semaphore.Release();
    }
}
