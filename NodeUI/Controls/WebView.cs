namespace NodeUI.Controls;

/// <summary>
/// Lazy-loading wrapper for <see cref="CefNet.Avalonia.WebView"/> to not immediately crash when debugging.
/// TODO: remove if debugging with CEF fixed
/// </summary>
public class WebView : Avalonia.Controls.Decorator
{
    public readonly CefNet.Avalonia.WebView View;

    public WebView()
    {
        View = new();

        if (!Debugger.IsAttached) init();
        else
        {
            Child = createtb("WebView placeholder; Hover to enable");
            Child.PointerEnter += (obj, e) => init();
        }


        static TextBlock createtb(string text) =>
            new TextBlock()
            {
                Background = Colors.AlmostTransparentWhite,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Text = text,
            };

        void init()
        {
            try
            {
                Program.InitializeCef();
                Child = View;
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error($"Could not initialize CEF: {ex}");

                Child = createtb($"Could not initialize CEF: {ex.Message}");
                ((TextBlock) Child).Foreground = Brushes.Red;
            }
        }
    }
}
