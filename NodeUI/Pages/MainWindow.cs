using System.Web;
using Avalonia.Controls.Templates;
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
            _ = StartStateListener(CancellationToken.None);


            var tabs = new TabbedControl();
            tabs.Add("tab.dashboard", new DashboardTab());
            tabs.Add("tab.plugins", new PluginsTab());
            tabs.Add("tab.benchmark", new BenchmarkTab());
            tabs.Add("menu.settings", new SettingsTab());
            tabs.Add("torrent test", new TorrentTab());

            Content = tabs;
        }

        void SubscribeToStateChanges()
        {
            IMessageBox? benchmb = null;
            NodeGlobalState.Instance.ExecutingBenchmarks.Changed += benchs => Dispatcher.UIThread.Post(() =>
            {
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
        async Task StartStateListener(CancellationToken token)
        {
            if (Init.IsDebug)
                try
                {
                    var cachefile = Path.Combine(Init.ConfigDirectory, "nodeinfocache");
                    if (File.Exists(cachefile))
                    {
                        try { JsonConvert.PopulateObject(File.ReadAllText(cachefile), NodeGlobalState.Instance, LocalApi.JsonSettingsWithType); }
                        catch { }
                    }

                    NodeGlobalState.Instance.AnyChanged.Subscribe(NodeGlobalState.Instance, _ =>
                        File.WriteAllText(cachefile, JsonConvert.SerializeObject(NodeGlobalState.Instance, LocalApi.JsonSettingsWithType)));
                }
                catch { }


            var consecutive = 0;
            while (true)
            {
                try
                {
                    var stream = await LocalPipe.SendLocalAsync("getstate").ConfigureAwait(false);
                    var reader = LocalPipe.CreateReader(stream);
                    consecutive = 0;

                    while (true)
                    {
                        var read = reader.Read();
                        if (!read) break;
                        if (token.IsCancellationRequested) return;

                        var jtoken = JToken.Load(reader);
                        _logger.Debug($"Node state updated: {jtoken.ToString(Formatting.None)}");

                        using var tokenreader = jtoken.CreateReader();
                        LocalApi.JsonSerializerWithType.Populate(tokenreader, NodeGlobalState.Instance);
                    }
                }
                catch (Exception ex)
                {
                    if (consecutive < 3) _logger.Error($"Could not read node state: {ex.Message}, reconnecting...");
                    else if (consecutive == 3) _logger.Error($"Could not read node state after {consecutive} retries, disabling connection retry logging...");

                    consecutive++;
                }

                await Task.Delay(1_000).ConfigureAwait(false);
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
                        Children = {
                            new OurPlugins(),
                            new SoftwareStats(),
                        },
                    },
                };

                Children.Add(scroll);
            }


            class SoftwareStats : Panel
            {
                readonly TextBlock InfoTextBlock;
                readonly StackPanel ItemsPanel;

                public SoftwareStats()
                {
                    InfoTextBlock = new();
                    ItemsPanel = new();

                    Children.Add(new Grid()
                    {
                        RowDefinitions = RowDefinitions.Parse("Auto *"),
                        Children =
                        {
                            InfoTextBlock.WithRow(0),
                            ItemsPanel.WithRow(1),
                        },
                    });


                    UICache.StartUpdatingStats();
                    UICache.SoftwareStats.SubscribeChanged((_, stats) => Dispatcher.UIThread.Post(() =>
                    {
                        InfoTextBlock.Text = $"SOFTWARE STATS\nLast update: {DateTimeOffset.Now}";
                        ItemsPanel.Children.Clear();
                        foreach (var (type, stat) in stats.OrderByDescending(x => x.Value.Total).ThenByDescending(x => x.Value.ByVersion.Count))
                        {
                            ItemsPanel.Children.Add(new Expander()
                            {
                                Header = $"{type} ({stat.Total} total installs; {stat.ByVersion.Count} different versions; {stat.ByVersion.Sum(x => (long) x.Value.Total)} total installed versions)",
                                Content = new ItemsControl()
                                {
                                    Items = stat.ByVersion.OrderByDescending(x => x.Value.Total).Select(v => $"{v.Key} ({v.Value.Total})"),
                                },
                            });
                        }
                    }), true);
                }
            }
            class OurPlugins : Panel
            {
                public OurPlugins()
                {
                    var infotext = new TextBlock();

                    var items = new ListBox()
                    {
                        ItemTemplate = new FuncDataTemplate<Plugin>((plugin, _) => new TextBlock() { Text = $"{plugin.Type} {plugin.Version}: {plugin.Path}" }),
                    };

                    Children.Add(new Grid()
                    {
                        RowDefinitions = RowDefinitions.Parse("Auto *"),
                        Children =
                        {
                            infotext.WithRow(0),
                            items.WithRow(1),
                        },
                    });

                    NodeGlobalState.Instance.InstalledPlugins.SubscribeChanged(info => Dispatcher.UIThread.Post(() =>
                    {
                        infotext.Text = $"OUR PLUGINS\nLast update: {DateTimeOffset.Now}";
                        items.Items = info;
                    }), true);
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
                        Children = {
                            new ExecutingTasks(),
                        },
                    },
                };

                Children.Add(scroll);
            }


            class ExecutingTasks : Panel
            {
                public ExecutingTasks()
                {
                    var infotext = new TextBlock();

                    var items = new ListBox()
                    {
                        ItemTemplate = new FuncDataTemplate<ReceivedTask>((task, _) => new TextBlock() { Text = $"{task.Id} {task.Info.TaskType}" }),
                    };

                    Children.Add(new Grid()
                    {
                        RowDefinitions = RowDefinitions.Parse("Auto *"),
                        Children =
                        {
                            infotext.WithRow(0),
                            items.WithRow(1),
                        },
                    });

                    NodeGlobalState.Instance.ExecutingTasks.SubscribeChanged(info => Dispatcher.UIThread.Post(() =>
                    {
                        infotext.Text = $"EXECUTING TASKS\nLast update: {DateTimeOffset.Now}";
                        items.Items = info;
                    }), true);
                }
            }
            class PlacedTasks : Panel
            {
                public PlacedTasks()
                {
                    var infotext = new TextBlock();

                    var items = new ListBox()
                    {
                        ItemTemplate = new FuncDataTemplate<PlacedTask>((task, _) => new TextBlock() { Text = $"{task.Id} {task.Info.Type}" }),
                    };

                    Children.Add(new Grid()
                    {
                        RowDefinitions = RowDefinitions.Parse("Auto *"),
                        Children =
                        {
                            infotext.WithRow(0),
                            items.WithRow(1),
                        },
                    });

                    NodeGlobalState.Instance.PlacedTasks.SubscribeChanged(info => Dispatcher.UIThread.Post(() =>
                    {
                        infotext.Text = $"PLACED TASKS\nLast update: {DateTimeOffset.Now}";
                        items.Items = info;
                    }), true);
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
                Settings.BNodeName.SubscribeChanged((oldv, newv) => Dispatcher.UIThread.Post(() => nicktb.Text = newv), true);

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
    }
}