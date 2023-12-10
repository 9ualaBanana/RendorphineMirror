using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NodeCommon;

// workaround for Windows until https://github.com/dotnet/runtime/issues/3828
public static class ConsoleHide
{
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void Hide()
    {
        if (Debugger.IsAttached) return;

#if DEBUG
        var one = 1;
        if (one == 1) return;
#endif

        const int sw_hide = 0;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ShowWindow(GetConsoleWindow(), sw_hide);
    }
}
