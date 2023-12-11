namespace Node.UI.State;

public class DebugNodeStateUpdaterWindow : Window
{
    readonly DebugNodeStateUpdater DebugNodeStateUpdater;

    public DebugNodeStateUpdaterWindow(DebugNodeStateUpdater debugNodeStateUpdater)
    {
        DebugNodeStateUpdater = debugNodeStateUpdater;

        Content = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new MPButton()
                {
                    Text = "Connect",
                    OnClick = DebugNodeStateUpdater.Connect,
                },
                new MPButton()
                {
                    Text = "Disconnect",
                    OnClick = DebugNodeStateUpdater.Disconnect,
                },
            },
        };
    }
}
