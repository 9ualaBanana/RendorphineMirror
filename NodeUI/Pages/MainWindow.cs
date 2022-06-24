using System.Web;
using Avalonia.Markup.Xaml.Templates;
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
            _ = StartStateListener();


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
            GlobalState.SubscribeChanged<BenchmarkNodeState>(
                (oldstate, newstate) => Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (newstate is BenchmarkNodeState bns)
                    {
                        if (benchmb is null)
                        {
                            benchmb = new MessageBox(new("Hide"));
                            benchmb.Show();
                        }

                        benchmb.Text = new(@$"
                            Benchmarking your system...
                            {bns.Completed.Count} completed: {string.Join(", ", bns.Completed)}
                        ".TrimLines());
                    }
                    else
                    {
                        benchmb?.Close();
                        benchmb = null;
                    }
                })
            );
        }
        async Task StartStateListener()
        {
            while (true)
            {
                await Task.Delay(2000).ConfigureAwait(false);

                var stateres = await LocalApi.Send("getstate", new { State = (INodeState) null! }).ConfigureAwait(false);
                stateres.LogIfError("Could not get node state: {0}");
                if (!stateres) continue;

                var oldstate = GlobalState.State.Value;
                var newstate = stateres.Value.State ?? IdleNodeState.Instance;
                if (newstate.GetType() != oldstate.GetType())
                    Log.Information($"Changing state from {oldstate.GetName()} to {newstate.GetName()}");

                GlobalState.State.Value = newstate;
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
                GlobalState.State.Changed += (_, _) => updatetext();

                var langbtn = new MPButton()
                {
                    MaxWidth = 100,
                    MaxHeight = 30,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Text = Localized.Lang.Current,
                    OnClick = () => Settings.Language = Settings.Language == "ru-RU" ? "en-US" : "ru-RU",
                };
                Children.Add(langbtn);


                void updatetext()
                {
                    var values = typeof(Settings).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                        .Where(x => x.FieldType.IsAssignableTo(typeof(IBindable)))
                        .Select(x => x.Name.Substring(1) + ": " + x.GetValue(null)!.GetType().GetProperty("Value")!.GetValue(x.GetValue(null)));

                    Dispatcher.UIThread.Post(() => infotb.Text =
                        @$"
                        Current node state: {GlobalState.State.Value.GetName()}
                        State data: {JsonConvert.SerializeObject(GlobalState.State.Value, Formatting.None)}

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
                Children.Add(new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "im plugin tab hello",
                });

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


                    new Thread(() =>
                    {
                        while (true)
                        {
                            _ = Load();
                            Thread.Sleep(/*60 * */60 * 1000);
                        }
                    })
                    { IsBackground = true }.Start();
                }

                async Task Load()
                {
                    var data = await Api.GetSoftwareStatsAsync().ConfigureAwait(false);
                    data.LogIfError();
                    if (!data) return;

                    await Dispatcher.UIThread.InvokeAsync(() => Set(data.Value)).ConfigureAwait(false);
                }
                void Set(ImmutableDictionary<string, Api.SoftwareStats> stats)
                {
                    InfoTextBlock.Text = $"Last update: {DateTimeOffset.Now}";

                    foreach (var (statname, stat) in stats.OrderByDescending(x => x.Value.Total).ThenByDescending(x => x.Value.ByVersion.Count))
                    {
                        ItemsPanel.Children.Add(new Expander()
                        {
                            Header = $"{getName(statname)} ({stat.Total})",
                            Content = new ItemsControl()
                            {
                                Items = stat.ByVersion.OrderByDescending(x => x.Value.Total).Select(v => $"{v.Key} ({v.Value.Total})"),
                            },
                        });
                    }


                    static string getName(string shortname) => shortname switch
                    {
                        "blender" => "Blender",
                        "autodesk3dsmax" => "Autodesk 3ds Max",
                        "topazgigapixelai" => "Topaz Gigapixel AI",
                        "davinciresolve" => "Davinci Resolve",
                        { } name when name.Length != 0 => char.ToUpper(name[0]) + name[1..],
                        { } name => name,
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
                Settings.BNodeName.SubscribeChanged((oldv, newv) => Dispatcher.UIThread.Post(() => nicktb.Text = newv), true);

                var nicksbtn = new MPButton()
                {
                    Text = new("set nick"),
                };
                nicksbtn.OnClick += async () =>
                {
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