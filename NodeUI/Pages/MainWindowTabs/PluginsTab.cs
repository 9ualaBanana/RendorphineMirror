namespace NodeUI.Pages.MainWindowTabs;

public class PluginsTab : Panel
{
    public PluginsTab()
    {
        var scroll = new ScrollViewer()
        {
            Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 20,
                Children =
                {
                    new InstallPluginPanel(),
                    new Panel() { Background = Colors.Black, Width = 400, },
                    NamedList.Create("Software stats", NodeGlobalState.Instance.SoftwareStats, softToControl),
                    NamedList.Create("Our plugins", NodeGlobalState.Instance.InstalledPlugins, pluginToControl),
                },
            },
        };

        Children.Add(scroll);


        IControl softToControl(KeyValuePair<string, SoftwareStats> value)
        {
            var (type, stat) = value;

            return new Expander()
            {
                Header = $"{type} ({stat.Total} total installs; {stat.ByVersion.Count} different versions; {stat.ByVersion.Sum(x => (long) x.Value.Total)} total installed versions)",
                Content = new ItemsControl()
                {
                    Items = stat.ByVersion.OrderByDescending(x => x.Value.Total).Select(v => $"{v.Key} ({v.Value.Total})"),
                },
            };
        }
        IControl pluginToControl(Plugin plugin) => new TextBlock() { Text = $"{plugin.Type} {plugin.Version}: {plugin.Path}" };
    }


    class InstallPluginPanel : Panel
    {
        readonly object Bindable;

        public InstallPluginPanel()
        {
            var versionslist = TypedComboBox.Create(Array.Empty<string>());
            versionslist.SelectedIndex = 0;

            var pluginslist = TypedComboBox.Create(Array.Empty<string>());
            pluginslist.SelectionChanged += (obj, e) => versionslist.Items = NodeGlobalState.Instance.SoftwareStats.Value.GetValueOrDefault(pluginslist.SelectedItem)?.ByVersion.Keys.ToArray() ?? Array.Empty<string>();
            pluginslist.SelectedIndex = 0;

            var cp = NodeGlobalState.Instance.SoftwareStats.GetBoundCopy();
            cp.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
            {
                pluginslist.Items = NodeGlobalState.Instance.SoftwareStats.Value.Keys.ToArray();
                pluginslist.SelectedIndex = pluginslist.SelectedIndex;
            }), true);
            Bindable = cp;

            var installbtn = new MPButton()
            {
                Text = "Install plugin",
                OnClickSelf = async self =>
                {
                    var res = await LocalApi.Default.Get("deploy", "Installing plugin", ("type", pluginslist.SelectedItem), ("version", versionslist.SelectedItem));
                    await self.FlashErrorIfErr(res);
                },
            };

            var panel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Children = { pluginslist, versionslist, installbtn },
            };
            Children.Add(panel);
        }
    }
}
