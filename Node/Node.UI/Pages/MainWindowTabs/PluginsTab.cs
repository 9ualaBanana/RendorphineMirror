using NodeCommon.NodeUserSettings;

namespace Node.UI.Pages.MainWindowTabs;

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
                    new UserSettingsPluginPanel().Named("User settings (temp)"),
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
                        // stat.SoftRequirements.Parents.Length == 0 ? new Control() : new TextBlock() { Text = "Parents: " + string.Join(", ", stat.SoftRequirements.Parents) },
                        // stat.SoftRequirements.Platforms.Count == 0 ? new Control() : new TextBlock() { Text = "Requirements: " + string.Join(", ", stat.SoftRequirements.Platforms)  },
                        stat.Versions.Count == 0 ? new Control() : new TextBlock()
                        {
                            Text = $"Versions: {string.Join(", ", stat.Versions.Select(v=>v.Key + "\n"+versionToString(v.Value)))}",
                        },
                    },
                },
            };


            string versionToString(SoftwareVersionDefinition version) => $"""
                    Requirements:
                        {string.Join(", ", version.Requirements.Platforms)}
                        {string.Join(", ", version.Requirements.Parents)}
                """;
        }
        IControl pluginToControl(Plugin plugin) => new TextBlock() { Text = $"{plugin.Type} {plugin.Version}: {plugin.Path}" };
    }


    class InstallPluginPanel : Panel
    {
        readonly IBindable GCBindable;

        public InstallPluginPanel()
        {
            var stats = NodeGlobalState.Instance.Software.GetBoundCopy();
            GCBindable = stats;

            var versionslist = TypedComboBox.Create(Array.Empty<string>()).With(c => c.MinWidth = 100);
            versionslist.SelectedIndex = 0;

            var pluginslist = TypedComboBox.Create(Array.Empty<string>()).With(c => c.MinWidth = 100);
            pluginslist.SelectionChanged += (obj, e) => versionslist.Items = stats.Value.GetValueOrDefault(pluginslist.SelectedItem ?? "")?.Versions.Keys.ToArray() ?? Array.Empty<string>();
            pluginslist.SelectedIndex = 0;

            stats.SubscribeChanged(() => Dispatcher.UIThread.Post(() => pluginslist.Items = stats.Value.Keys.ToArray()), true);

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
    class UserSettingsPluginPanel : Panel
    {
        readonly IBindable<UserSettings2> Settings;
        readonly IBindable<ImmutableDictionary<string, SoftwareDefinition>> Stats;

        public UserSettingsPluginPanel()
        {
            Settings = NodeGlobalState.Instance.UserSettings.GetBoundCopy();
            Stats = NodeGlobalState.Instance.Software.GetBoundCopy();

            Apis.Default.GetSettings2Async()
                .Next(s => { Settings.Value = s; return true; });

            var versionslist = TypedComboBox.Create(Array.Empty<string>()).With(c => c.MinWidth = 100);
            versionslist.SelectedIndex = 0;

            var pluginslist = TypedComboBox.Create(Array.Empty<string>()).With(c => c.MinWidth = 100);
            pluginslist.SelectionChanged += (obj, e) => versionslist.Items = Stats.Value.GetValueOrDefault(pluginslist.SelectedItem ?? "")?.Versions.Keys.ToArray() ?? Array.Empty<string>();
            pluginslist.SelectedIndex = 0;

            Stats.SubscribeChanged(() => Dispatcher.UIThread.Post(() => pluginslist.Items = Stats.Value.Keys.ToArray()), true);


            var stack = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new TextBlock().With(t =>
                    {
                        Settings.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
                        {
                            t.Text = $"""
                                Settings:
                                {string.Join('\n', Settings.Value.NodeInstallSoftware?.Select(k => $"{k.Key}: {pluginsToString(k.Value)}") ?? Enumerable.Empty<object>())}
                                everyone: {string.Join(", ", pluginsToString(Settings.Value.InstallSoftware ?? new()))}
                                """;

                            string pluginsToString(UserSettings2.TMServerSoftware soft) =>
                                string.Join(", ", soft.Select(k => $"{k.Key} ({string.Join(", ", k.Value.Keys.Select(v => v.Length == 0 ? "latest" : v))})"));
                        }), true);
                    }),
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            // TODO: move this to hearbeat or smth
                            new MPButton()
                            {
                                Text = "Reload settings",
                                OnClickSelf = async self =>
                                {
                                    var settings = await Apis.Default.GetSettings2Async();
                                    if (settings)
                                        Settings.Value = settings.Value;

                                    await self.Flash(settings);
                                },
                            },
                            pluginslist,
                            versionslist,

                            new MPButton()
                            {
                                Text = "Install (this node)",
                                OnClickSelf = async self =>
                                {
                                    Settings.Value.Install(NodeGlobalState.Instance.AuthInfo.ThrowIfNull().Guid, Enum.Parse<PluginType>(pluginslist.SelectedItem, true), versionslist.SelectedItem);
                                    Settings.TriggerValueChanged();

                                    var set = await Apis.Default.SetSettingsAsync(Settings.Value);
                                    await self.Flash(set);
                                },
                            },
                            new MPButton()
                            {
                                Text = "Uninstall (this node)",
                                OnClickSelf = async self =>
                                {
                                    Settings.Value.Uninstall(NodeGlobalState.Instance.AuthInfo.ThrowIfNull().Guid, Enum.Parse<PluginType>(pluginslist.SelectedItem, true), versionslist.SelectedItem);
                                    Settings.TriggerValueChanged();

                                    var set = await Apis.Default.SetSettingsAsync(Settings.Value);
                                    await self.Flash(set);
                                },
                            },
                            new MPButton()
                            {
                                Text = "Install (all nodes)",
                                OnClickSelf = async self =>
                                {
                                    Settings.Value.Install(Enum.Parse<PluginType>(pluginslist.SelectedItem, true), versionslist.SelectedItem);
                                    Settings.TriggerValueChanged();

                                    var set = await Apis.Default.SetSettingsAsync(Settings.Value);
                                    await self.Flash(set);
                                },
                            },
                            new MPButton()
                            {
                                Text = "Uninstall (all nodes)",
                                OnClickSelf = async self =>
                                {
                                    Settings.Value.Uninstall(Enum.Parse<PluginType>(pluginslist.SelectedItem, true), versionslist.SelectedItem);
                                    Settings.TriggerValueChanged();

                                    var set = await Apis.Default.SetSettingsAsync(Settings.Value);
                                    await self.Flash(set);
                                },
                            },
                        },
                    },
                },
            };
            Children.Add(stack);
        }
    }
}
