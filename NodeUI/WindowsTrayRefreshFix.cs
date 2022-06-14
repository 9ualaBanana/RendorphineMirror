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

            try
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
                if (foor > 0)
                {
                    System.Drawing.Rectangle r = new System.Drawing.Rectangle();
                    GetWindowRect(foor, ref r);

                    for (int x = 0; x < (r.Right - r.Left) - r.X; x += 8)
                        for (int y = 0; y < (r.Bottom - r.Top) - r.Y; y += 8)
                            SendMessage(foor, WM_MOUSEMOVE, 0, x | (y << 16));
                }
            }
            catch (Exception ex) { Log.Error(ex.ToString()); }
        }
    }
}