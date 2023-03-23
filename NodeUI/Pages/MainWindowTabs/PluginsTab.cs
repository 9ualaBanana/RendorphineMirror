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
                    new InstallPluginPanel().Named("Install plugins"),
                    NamedList.Create("Stats", NodeGlobalState.Instance.SoftwareStats, softStatToControl),
                    NamedList.Create("Registry", NodeGlobalState.Instance.Software, softToControl),
                    NamedList.Create("Installed", NodeGlobalState.Instance.InstalledPlugins, pluginToControl),
                },
            },
        };

        Children.Add(scroll);


        IControl softStatToControl(KeyValuePair<string, SoftwareStats> value)
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
        IControl softToControl(KeyValuePair<string, SoftwareDefinition> value)
        {
            var (type, stat) = value;

            return new Expander()
            {
                Header = $"{type} \"{stat.VisualName}\"",
                Content = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        stat.Parents.Length == 0 ? new Control() : new TextBlock() { Text = "Parents: " + string.Join(", ", stat.Parents) },
                        stat.Requirements is null ? new Control() : new TextBlock() { Text = "Requirements: " + "Windows " + stat.Requirements.WindowsVersion },
                        stat.Versions.Count == 0 ? new Control() : new TextBlock() { Text = "Versions: " + string.Join(", ", stat.Versions.Keys) },
                    },
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
            var versionslist = TypedComboBox.Create(Array.Empty<string>()).With(c => c.MinWidth = 100);
            versionslist.SelectedIndex = 0;

            var pluginslist = TypedComboBox.Create(Array.Empty<string>()).With(c => c.MinWidth = 100);
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
                Text = "Install",
                OnClickSelf = async self =>
                {
                    var res = await LocalApi.Default.Get("deploy", "Installing plugin", ("type", pluginslist.SelectedItem), ("version", versionslist.SelectedItem));
                    await self.FlashErrorIfErr(res);
                },
            };

            var panel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children = { pluginslist, versionslist, installbtn },
            };
            Children.Add(panel);
        }
    }
}
