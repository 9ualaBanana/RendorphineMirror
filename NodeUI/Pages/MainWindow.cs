using System.Net.Http.Headers;
using System.Web;
using MonoTorrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeUI.Pages
{
    public class MainWindow : Window
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            Width = 692;
            Height = 410;
            Title = App.AppName;
            Icon = App.Icon;

            this.PreventClosing();
            SubscribeToStateChanges();


            var tabs = new TabbedControl();
            tabs.Add("tab.dashboard", new DashboardTab());
            tabs.Add("tab.plugins", new PluginsTab());
            tabs.Add("tasks", new TasksTab());
            tabs.Add("tab.benchmark", new BenchmarkTab());
            tabs.Add("menu.settings", new SettingsTab());
            tabs.Add("torrent test", new TorrentTab());
            if (Init.IsDebug)
                tabs.Add("registry", new RegistryTab());

            Content = tabs;
        }

        void SubscribeToStateChanges()
        {
            IMessageBox? benchmb = null;
            NodeGlobalState.Instance.ExecutingBenchmarks.Changed += () => Dispatcher.UIThread.Post(() =>
            {
                var benchs = NodeGlobalState.Instance.ExecutingBenchmarks;

                if (benchs.Count != 0)
                {
                    if (benchmb is null)
                    {
                        benchmb = new MessageBox(new("Hide"));
                        benchmb.Show();
                    }

                    benchmb.Text = new(@$"
                        Benchmarking your system...
                        {benchs.Count} completed: {JsonConvert.SerializeObject(benchs)}
                    ".TrimLines());
                }
                else
                {
                    benchmb?.Close();
                    benchmb = null;
                }
            });
        }


        static class NamedList
        {
            public static NamedList<T> Create<T>(string title, IReadOnlyCollection<T> items, Func<T, IControl> templatefunc) => new(title, items, templatefunc);
        }
        class NamedList<T> : Grid
        {
            // GC protected instance
            readonly IReadOnlyCollection<T> Items;

            public NamedList(string title, IReadOnlyCollection<T> items, Func<T, IControl> templatefunc)
            {
                Items = items = (items as IReadOnlyBindableCollection<T>)?.GetBoundCopy() ?? items;

                Children.Add(new Grid()
                {
                    RowDefinitions = RowDefinitions.Parse("Auto *"),
                    Children =
                    {
                        new TextBlock()
                            .With(tb => (items as IReadOnlyBindableCollection<T>)?.SubscribeChanged(() => Dispatcher.UIThread.Post(() => tb.Text = $"{title}\nLast update: {DateTimeOffset.Now}"), true))
                            .WithRow(0),

                        TypedItemsControl.Create(items, templatefunc)
                            .WithRow(1),
                    },
                });
            }
        }

        class DashboardTab : Panel
        {
            public DashboardTab()
            {
                var starttime = DateTimeOffset.Now;
                var infotb = new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                };
                Children.Add(infotb);
                updatetext();
                Settings.AnyChanged += updatetext;
                NodeGlobalState.Instance.AnyChanged.Subscribe(this, _ => updatetext());

                var langbtn = new MPButton()
                {
                    MaxWidth = 100,
                    MaxHeight = 30,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Text = "lang.current",
                    OnClick = () => UISettings.Language = UISettings.Language == "ru-RU" ? "en-US" : "ru-RU",
                };
                Children.Add(langbtn);

                var unloginbtn = new MPButton()
                {
                    MaxWidth = 100,
                    MaxHeight = 30,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(200, 0, 0, 0),
                    Text = new("unlogin"),
                    OnClick = () =>
                    {
                        Settings.AuthInfo = null;
                        Settings.NodeName = null!;

                        LocalApi.Send("reloadcfg").AsTask().Consume();
                        new LoginWindow().Show();
                        ((Window) VisualRoot!).Close();
                    },
                };
                Children.Add(unloginbtn);

                if (Settings.IsSlave == false)
                {
                    var taskbtn = new MPButton()
                    {
                        Text = new("new task"),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        OnClick = () => new TaskCreationWindow().Show(),
                    };
                    Children.Add(taskbtn);
                }


                void updatetext()
                {
                    var values = typeof(Settings).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                        .Where(x => x.FieldType.IsAssignableTo(typeof(IBindable)))
                        .Select(x => x.Name.Substring(1) + ": " + x.GetValue(null)!.GetType().GetProperty("Value")!.GetValue(x.GetValue(null)));

                    Dispatcher.UIThread.Post(() => infotb.Text =
                        @$"
                        Current node state: {JsonConvert.SerializeObject(NodeGlobalState.Instance, Formatting.Indented)}

                        Settings:
                        {string.Join("; ", values)}
                        Ui start time: {starttime}
                        ".TrimLines()
                    );
                }
            }
        }
        class PluginsTab : Panel
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
                            NamedList.Create("Software stats", UICache.SoftwareStats, softToControl),
                            NamedList.Create("Our plugins", NodeGlobalState.Instance.InstalledPlugins, pluginToControl),
                        },
                    },
                };

                Children.Add(scroll);


                IControl softToControl(KeyValuePair<PluginType, SoftwareStats> value)
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
                    pluginslist.SelectionChanged += (obj, e) => versionslist.Items = UICache.SoftwareStats[Enum.Parse<PluginType>(pluginslist.SelectedItem ?? PluginType.FFmpeg.ToString())].ByVersion.Select(x => x.Key).ToArray();
                    pluginslist.SelectedIndex = 0;

                    var cp = UICache.SoftwareStats.GetBoundCopy();
                    cp.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
                    {
                        pluginslist.Items = UICache.SoftwareStats.Select(x => x.Key.ToString()).ToArray();
                        pluginslist.SelectedIndex = pluginslist.SelectedIndex;
                    }), true);
                    Bindable = cp;

                    var installbtn = new MPButton()
                    {
                        Text = "Install plugin",
                        OnClickSelf = async self =>
                        {
                            var res = await LocalApi.Send($"deploy?type={HttpUtility.UrlEncode(pluginslist.SelectedItem)}&version={HttpUtility.UrlEncode(versionslist.SelectedItem)}");
                            if (!res) await self.TemporarySetText("err " + res.AsString());
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
        class TasksTab : Panel
        {
            public TasksTab()
            {
                var scroll = new ScrollViewer()
                {
                    Content = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 20,
                        Children =
                        {
                            NamedList.Create("Executing tasks", NodeGlobalState.Instance.ExecutingTasks, execTasksCreate),
                            NamedList.Create("Watching tasks", NodeGlobalState.Instance.WatchingTasks, watchingTasksCreate),
                            NamedList.Create("Placed tasks", NodeGlobalState.Instance.PlacedTasks, placedTasksCreate),
                        },
                    },
                };

                Children.Add(scroll);


                IControl execTasksCreate(ReceivedTask task)
                {
                    var statustb = new TextBlock();

                    return new Expander()
                    {
                        Header = $"{task.Id} {task.GetPlugin()} {task.Action}",
                        Content = new StackPanel()
                        {
                            Orientation = Orientation.Vertical,
                            Children =
                            {
                                new TextBlock() { Text = $"Data: {task.Info.Data.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Input: {task.Info.Input.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Output: {task.Info.Output.ToString(Formatting.None)}" },
                                statustb,
                            },
                        },
                    };
                }
                IControl placedTasksCreate(DbTaskFullState task)
                {
                    var statustb = new TextBlock();
                    var statusbtn = new MPButton()
                    {
                        Text = "Update status",
                        OnClick = async () =>
                        {
                            var state = await task.GetTaskStateAsync();
                            if (!state)
                            {
                                statustb.Text = "error " + state.AsString();
                                return;
                            }

                            statustb.Text = JsonConvert.SerializeObject(state.Value, Formatting.None);
                        },
                    };

                    return new Expander()
                    {
                        Header = $"{task.Id} {task.GetPlugin()} {task.Action}",
                        Content = new StackPanel()
                        {
                            Orientation = Orientation.Vertical,
                            Children =
                            {
                                new TextBlock() { Text = $"Data: {task.Info.Data.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Input: {task.Info.Input.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Output: {task.Info.Output.ToString(Formatting.None)}" },
                                statustb,
                                statusbtn,
                            },
                        },
                    };
                }
                IControl watchingTasksCreate(WatchingTaskInfo task)
                {
                    return new Expander()
                    {
                        Header = $"{task.Id} {NodeGlobalState.Instance.GetPluginTypeFromAction(task.TaskAction)} {task.TaskAction}",
                        Content = new StackPanel()
                        {
                            Orientation = Orientation.Vertical,
                            Children =
                            {
                                new TextBlock() { Text = $"Data: {task.TaskData.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Source: {task.Source.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Output: {task.Output.ToString(Formatting.None)}" },
                            },
                        },
                    };
                }
            }
        }
        class BenchmarkTab : Panel
        {
            public BenchmarkTab()
            {
                Background = Brushes.Aqua;
                Children.Add(new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "beach mark",
                });
            }
        }
        class SettingsTab : Panel
        {
            public SettingsTab()
            {
                var nick = CreateNick();
                nick.MaxWidth = 400;
                nick.MaxHeight = 200;
                Children.Add(nick);
            }

            Grid CreateNick()
            {
                var nicktb = new TextBox()
                {

                };
                Settings.BNodeName.Bindable.SubscribeChanged(() => Dispatcher.UIThread.Post(() => nicktb.Text = Settings.NodeName), true);

                var nicksbtn = new MPButton()
                {
                    Text = new("set nick"),
                };
                nicksbtn.OnClick += async () =>
                {
                    using var _ = new FuncDispose(() => Dispatcher.UIThread.Post(() => nicksbtn.IsEnabled = true));
                    nicksbtn.IsEnabled = false;

                    var nick = nicktb.Text.Trim();
                    if (Settings.NodeName == nick)
                    {
                        Dispatcher.UIThread.Post(() => nicksbtn.Text = new($"cant change nick to the same nick nick name nick nick name\nnick"));
                        return;
                    }

                    var set = await LocalApi.Send($"setnick?nick={HttpUtility.UrlEncode(nick)}").ConfigureAwait(false);
                    Settings.Reload();

                    if (set) Dispatcher.UIThread.Post(() => nicksbtn.Text = new($"nick change good, new nick = {Settings.NodeName}"));
                    else Dispatcher.UIThread.Post(() => nicksbtn.Text = new(set.AsString()));
                };

                return new Grid()
                {
                    RowDefinitions = RowDefinitions.Parse("* *"),
                    Children =
                    {
                        nicktb.WithRow(0),
                        nicksbtn.WithRow(1),
                    }
                };
            }
        }
        class TorrentTab : Panel
        {
            public TorrentTab() => CreateTorrentUI();

            void CreateTorrentUI()
            {
                var info = new TextBlock() { Text = "loading info..." };
                var info2 = new TextBlock() { Text = $"waiting for upload.." };

                var urltb = new TextBox() { Text = "URL" };
                var dirtb = new TextBox() { Text = "/home/i3ym/Документы/Projects/tzn/Debug/" };
                var button = new MPButton() { Text = new("send torrent") };
                button.OnClick += click;

                var torrentgrid = new Grid()
                {
                    Height = 300,
                    RowDefinitions = RowDefinitions.Parse("* * * * *"),
                    Children =
                    {
                        info.WithRow(0),
                        info2.WithRow(1),
                        urltb.WithRow(2),
                        dirtb.WithRow(3),
                        button.WithRow(4),
                    },
                };
                Children.Add(torrentgrid);

                PortForwarding.GetPublicIPAsync().ContinueWith(t => Dispatcher.UIThread.Post(() =>
                    info.Text = $"this pc:  pub ip: {t.Result}  pub port: {PortForwarding.Port}  torrent port: {TorrentClient.ListenPort}"
                ));


                async void click()
                {
                    var url = urltb.Text.Trim();
                    var dir = dirtb.Text.Trim();

                    if (!Directory.Exists(dir))
                    {
                        button.Text = new("err dir not found");
                        return;
                    }

                    try
                    {
                        var client = new HttpClient();
                        var get = await LocalApi.Send<string>($"uploadtorrent?url={HttpUtility.UrlEncode(url)}&dir={HttpUtility.UrlEncode(dir)}").ConfigureAwait(false);
                        if (!get)
                        {
                            Dispatcher.UIThread.Post(() => button.Text = new("err " + get.AsString()));
                            return;
                        }
                        var hash = InfoHash.FromHex(get.Value);

                        new Thread(async () =>
                        {
                            bool die = false;
                            while (true)
                            {
                                var info = await LocalApi.Send<JObject>(url, $"torrentinfo?hash={HttpUtility.UrlEncode(hash.ToHex())}").ConfigureAwait(false);
                                if (!info)
                                {
                                    Thread.Sleep(200);
                                    continue;
                                }
                                var ifo = info.Value;

                                try
                                {
                                    if (ifo["progress"]?.Value<double>() > 99)
                                        Dispatcher.UIThread.Post(() => info2.Text = ifo.ToString(Newtonsoft.Json.Formatting.None) + "\nUpload completed.");

                                    if (!die && ifo["progress"]?.Value<double>() > 99)
                                    {
                                        die = true;
                                        Thread.Sleep(500);
                                        continue;
                                    }
                                }
                                catch { }

                                if (die)
                                {
                                    await client.GetAsync($"http://127.0.0.1:{Settings.LocalListenPort}/stoptorrent?url={hash.ToHex()}").ConfigureAwait(false);
                                    break;
                                }

                                Dispatcher.UIThread.Post(() => info2.Text = ifo.ToString(Newtonsoft.Json.Formatting.None));
                                Thread.Sleep(200);
                            }
                        })
                        { IsBackground = true }.Start();
                    }
                    catch { Dispatcher.UIThread.Post(() => button.Text = new("LOCAL connection error")); }
                }
            }
        }
        class RegistryTab : Panel
        {
            public RegistryTab() => Reload().Consume();

            async Task Reload()
            {
                Children.Clear();

                var softlist = (await Apis.GetSoftwareAsync()).ThrowIfError();
                Children.Add(new StackPanel()
                {
                    Children =
                    {
                        new MPButton()
                        {
                            Text = "+ add soft",
                            Margin = new Thickness(0, 0, 0, bottom: 20),
                            OnClickSelf = addSoft,
                        },
                        NamedList.Create("Software Registry", softlist, x => softToControl(x.Key, x.Value)),
                    },
                });


                static Task setTextTimed(MPButton button, string text, int duration) => button.TemporarySetText(text, duration);
                async void addSoft(MPButton button)
                {
                    var softname = "NewSoftTodo";
                    var soft = new SoftwareDefinition("New Soft Todo", ImmutableDictionary<string, SoftwareVersionDefinition>.Empty, null, ImmutableArray<string>.Empty);

                    var send = await LocalApi.Post(Settings.RegistryUrl, $"addsoft?name={HttpUtility.UrlEncode(softname)}",
                        new StringContent(JsonConvert.SerializeObject(soft)) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                    if (!send) await setTextTimed(button, "err " + send.AsString(), 2000);
                    else await Reload();
                }
                IControl softToControl(string softname, SoftwareDefinition soft)
                {
                    // return TaskCreationWindow.Settings.Create(new("_aed_", JObject.FromObject(soft)), FieldDescriber.Create(typeof(SoftwareDefinition)));

                    var softnametb = new TextBox() { Text = soft.VisualName };
                    var addnewbtn = new MPButton()
                    {
                        Text = "+ add version",
                        OnClick = async () =>
                        {
                            var vername = "1.0.0-todo";
                            var ver = new SoftwareVersionDefinition("<installscript>");

                            var send = await LocalApi.Post(Settings.RegistryUrl, $"addver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}",
                                new StringContent(JsonConvert.SerializeObject(ver)) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                            await Reload();
                        },
                    };

                    var content = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            new MPButton() { Text = "update software", OnClickSelf = updateSoft, },
                            softnametb,
                            addnewbtn,
                            NamedList.Create(softname, soft.Versions,
                                x => new Expander()
                                {
                                    Header = x.Key,
                                    Margin = new Thickness(left: 20, 0, 0, 0),
                                    Content = verToControl(x.Key, x.Value),
                                }
                            ),
                        },
                    };

                    return new Expander()
                    {
                        Header = softname,
                        Content = content,
                    };


                    async void updateSoft(MPButton button)
                    {
                        var json = new JObject()
                        {
                            ["VisualName"] = softnametb.Text,
                        };

                        var send = await LocalApi.Post(Settings.RegistryUrl, $"editsoft?name={HttpUtility.UrlEncode(softname)}",
                            new StringContent(json.ToString()) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                        await setTextTimed(button, send ? "send!!!!" : ("err " + send.AsString()), 2000);
                    }
                    IControl verToControl(string vername, SoftwareVersionDefinition version)
                    {
                        var delbtn = new MPButton()
                        {
                            Text = "!!! DELETE VERSION !!!",
                            Margin = new Thickness(0, 0, 0, bottom: 20),
                            OnClickSelf = deleteVersion,
                        };

                        var versiontb = new TextBox() { Text = vername };
                        var installtb = new TextBox()
                        {
                            AcceptsReturn = true,
                            AcceptsTab = true,
                            Text = version.InstallScript,
                        };

                        var updatebtn = new MPButton() { Text = "send", OnClickSelf = updateVersion, };

                        return new StackPanel()
                        {
                            Margin = new Thickness(20, 0, 0, 0),
                            Orientation = Orientation.Vertical,
                            Children =
                            {
                                delbtn,
                                versiontb,
                                installtb,
                                updatebtn,
                            },
                        };


                        async void updateVersion(MPButton updatebtn)
                        {
                            var json = new JObject()
                            {
                                ["InstallScript"] = installtb.Text,
                            };

                            var send = await LocalApi.Post(Settings.RegistryUrl, $"editver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}&newversion={HttpUtility.UrlEncode(versiontb.Text)}",
                                new StringContent(json.ToString()) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                            await setTextTimed(updatebtn, send ? "send!!!!" : ("err " + send.AsString()), 2000);
                        }
                        async void deleteVersion(MPButton delbtn)
                        {
                            var send = await LocalApi.Send(Settings.RegistryUrl, $"delver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}");
                            if (!send) await setTextTimed(delbtn, "err " + send.AsString(), 2000);
                            else await Reload();
                        }
                    }
                }
            }
        }
    }
}