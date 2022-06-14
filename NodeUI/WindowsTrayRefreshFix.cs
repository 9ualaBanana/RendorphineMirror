using System.Runtime.InteropServices;

namespace NodeUI
{
    public static class WindowsTrayRefreshFix
    {
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        static extern int FindWindow(string lpszClass, string? lpszWindow);
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        static extern int FindWindowEx(int hwndParent, int hwndChildAfter, string lpszClass, string? lpszWindow);

        [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
        static extern int GetWindowRect(int hwnd, ref System.Drawing.Rectangle lpRect);
        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        static extern int SendMessage(int hwnd, int wMsg, int wParam, int lParam);

        const int WM_MOUSEMOVE = 512;

        public static void RefreshTrayArea()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            OperationResult.WrapException(UpdateTray).LogIfError();
            OperationResult.WrapException(UpdateHiddenTray).LogIfError();


            static void UpdateTray()
            {
                //Taskbar window
                int one = FindWindow("Shell_TrayWnd", null);

                //Tray icon on the right of taskbar + time zone
                int two = FindWindowEx(one, 0, "TrayNotifyWnd", null);

                //Different systems may not have this layer
                int three = FindWindowEx(two, 0, "SysPager", null);

                //Tray icon window
                int foor;
                if (three > 0) foor = FindWindowEx(three, 0, "ToolbarWindow32", null);
                else foor = FindWindowEx(two, 0, "ToolbarWindow32", null);

                Update(foor);
            }
            static void UpdateHiddenTray()
            {
                int iconw = FindWindow("NotifyIconOverflowWindow", null);
                int toolbar = FindWindowEx(iconw, 0, "ToolbarWindow32", null);

                Update(toolbar);
            }
            static void Update(int hwnd)
            {
                if (hwnd <= 0) return;

                var rect = new System.Drawing.Rectangle();
                GetWindowRect(hwnd, ref rect);

                for (int x = 0; x < (rect.Right - rect.Left) - rect.X; x += 8)
                    for (int y = 0; y < (rect.Bottom - rect.Top) - rect.Y; y += 8)
                        SendMessage(hwnd, WM_MOUSEMOVE, 0, x | (y << 16));
            }
        }
    }
}