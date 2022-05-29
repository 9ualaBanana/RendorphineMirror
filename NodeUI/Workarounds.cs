namespace NodeUI
{
    public static class Workarounds
    {
        // bug in 0.10.6+
        // Window.WindowStartupLocation doesn't work
        // TODO: remove when fixed
        public static void FixStartupLocation(this Window window) => window.Initialized += (_, _) => window.Show();
    }
}