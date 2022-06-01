namespace NodeUI
{
    // TODO: remove stuff when fixed
    public static class Workarounds
    {
        // bug in 0.10.6+
        // Window.WindowStartupLocation doesn't work
        public static void FixStartupLocation(this Window window) => window.Initialized += (_, _) => window.Show();

        // tray indicators throwing exception on window close
        // https://github.com/AvaloniaUI/Avalonia/issues/7588
        public static void FixException(this TrayIcon icon) => TrayIcon.SetIcons(Application.Current!, new TrayIcons() { icon });
    }
}