using Autofac;
using Node.UI.Pages.MainWindowTabs;

namespace Node.UI.Pages;

public class AdminWindow : Window
{
    public AdminWindow(IComponentContext container)
    {
        var tabs = new TabbedControl();
        tabs.Add("registry", container.Resolve<RegistryEditor>());

        Content = tabs;
    }
}
