namespace Node.UI.Pages;

public abstract class GuiRequestWindow : Window
{
    public event Action? Cancelled;
    bool DoClose = false;

    protected GuiRequestWindow()
    {
        this.AttachDevToolsIfDebug();

        Closing += (_, e) =>
        {
            if (DoClose) return;
            Cancelled?.Invoke();
        };
    }

    public void ForceClose()
    {
        DoClose = true;
        Close();
    }
}
