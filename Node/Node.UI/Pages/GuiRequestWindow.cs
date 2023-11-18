namespace Node.UI.Pages;

public abstract class GuiRequestWindow : Window
{
    bool DoClose = false;

    protected GuiRequestWindow()
    {
        this.AttachDevToolsIfDebug();

        Closing += (_, e) => e.Cancel |= !DoClose;
    }

    public void ForceClose()
    {
        DoClose = true;
        Close();
    }
}
