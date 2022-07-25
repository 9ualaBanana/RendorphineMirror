using System.Web;
using MonoTorrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeUI.Pages
{
    public class MainWindow : Window
    {
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
            _ = UICache.GetTasksInfoAsync();


            var tabs = new TabbedControl();
            tabs.Add(Localized.Tab.Dashboard, new DashboardTab());
            tabs.Add(Localized.Tab.Plugins, new PluginsTab());
            tabs.Add(Localized.Tab.Benchmark, new BenchmarkTab());
            tabs.Add(Localized.Menu.Settings, new SettingsTab());
            tabs.Add(new("torrent test"), new TorrentTab());

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
                        Log.Debug($"Node state updated: {jtoken.ToString(Formatting.None)}");

                        using var tokenreader = jtoken.CreateReader();
                        LocalApi.JsonSerializerWithType.Populate(tokenreader, NodeGlobalState.Instance);
                    }
                }
                catch (Exception ex)
                {
                    if (consecutive < 3) Log.Error($"Could not read node state: {ex.Message}, reconnecting...");
                    else if (consecutive == 3) Log.Error($"Could not read node state after {consecutive} retries, disabling connection retry logging...");

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
                NodeGlobalState.Instance.AnyChanged.Subscribe(this, updatetext);

                var langbtn = new MPButton()
                {
                    MaxWidth = 100,
                    MaxHeight = 30,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Text = Localized.Lang.Current,
                    OnClick = () => UISettings.Language = UISettings.Language == "ru-RU" ? "en-US" : "ru-RU",
                };
                Children.Add(langbtn);

                var taskbtn = new MPButton()
                {
                    Text = new("new task"),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    OnClick = () => new TaskCreationWindow().Show(),
                };
                Children.Add(taskbtn);


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
                Children.Add(new SoftwareStats());
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
                        InfoTextBlock.Text = $"Last update: {DateTimeOffset.Now}";
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