using Avalonia.Controls.ApplicationLifetimes;

namespace Node.UI;

public class MainWindowUpdater
{
    public required IClassicDesktopStyleApplicationLifetime Lifetime { get; init; }
    public required NodeStateUpdater NodeStateUpdater { get; init; }
    public required Func<MainWindow> MainWindowCreator { get; init; }
    public required Func<LoginWindow> LoginWindowCreator { get; init; }

    public Window SetMainWindow()
    {
        if (Lifetime.MainWindow is MainWindow && NodeStateUpdater.IsConnectedToNode.Value && NodeGlobalState.Instance.AuthInfo?.SessionId is not null)
            return Lifetime.MainWindow;

        Lifetime.MainWindow?.Hide();
        return Lifetime.MainWindow =
            (!NodeStateUpdater.IsConnectedToNode.Value)
            ? new InitializingWindow()
            : NodeGlobalState.Instance.AuthInfo?.SessionId is null
                ? LoginWindowCreator()
                : MainWindowCreator();
    }
}
