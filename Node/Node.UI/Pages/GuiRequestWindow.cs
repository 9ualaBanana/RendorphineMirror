namespace Node.UI.Pages;

public abstract class GuiRequestWindow : Window
{
    bool DoClose = false;

    protected GuiRequestWindow() => Closing += (_, e) => e.Cancel |= !DoClose;

    public void ForceClose()
    {
        DoClose = true;
        Close();
    }
}
