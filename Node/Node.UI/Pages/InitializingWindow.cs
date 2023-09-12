namespace Node.UI.Pages;

public class InitializingWindow : Window
{
    public InitializingWindow()
    {
        Title = "Initializing...";
        Icon = App.Instance.Icon;

        Width = 200;
        Height = 100;

        Content = new TextBlock()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Text = $"Initializing Renderfin..." + (App.Instance.Init.IsDebug ? "\n(turn on node)" : null),
        };
    }
}
