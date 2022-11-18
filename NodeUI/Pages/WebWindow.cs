namespace NodeUI.Pages;

public class WebWindow : Window
{
    public CefNet.Avalonia.WebView View => ((WebView) Content).View;

    public WebWindow() => Content = new WebView();
}
