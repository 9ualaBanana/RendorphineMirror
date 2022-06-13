using System.Web;
using MonoTorrent;

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


            var tabs = new TabbedControl();
            tabs.Add(Localized.Tab.Dashboard, new DashboardTab());
            tabs.Add(Localized.Tab.Plugins, new PluginsTab());
            tabs.Add(Localized.Tab.Benchmark, new BenchmarkTab());
            tabs.Add(new("debug"), new DebugTab());

            Content = tabs;
        }


        class DashboardTab : Panel
        {
            public DashboardTab()
            {
                Children.Add(new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "dashboard window says hello world\nui start time: " + DateTimeOffset.Now,
                });

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
        class DebugTab : Panel
        {
            public DebugTab()
            {
                CreateTorrentUI();
            }

            void CreateTorrentUI()
            {
                var info = new TextBlock() { Text = $"this pc:  ip: {PortForwarding.GetPublicIPAsync().Result}  public port: {PortForwarding.Port}" };
                var info2 = new TextBlock() { Text = $"this pc:  ip: {PortForwarding.GetPublicIPAsync().Result}  public port: {PortForwarding.Port}" };

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
                        var get = await client.GetAsync($"http://127.0.0.1:{Settings.LocalListenPort}/uploadtorrent?url={url}&dir={HttpUtility.UrlEncode(dir)}").ConfigureAwait(false);
                        if (!get.IsSuccessStatusCode)
                        {
                            var err = await get.Content.ReadAsStringAsync().ConfigureAwait(false);
                            Dispatcher.UIThread.Post(() => button.Text = new("err " + get.StatusCode + " " + err));
                            return;
                        }

                        var hashbytes = await get.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        var hash = new InfoHash(hashbytes);

                        new Thread(async () =>
                        {
                            while (true)
                            {
                                var info = await client.GetAsync($"http://{url}/torrentinfo?hash={HttpUtility.UrlEncode(hash.ToHex())}").ConfigureAwait(false);
                                var ifo = await info.Content.ReadAsStringAsync().ConfigureAwait(false);

                                Dispatcher.UIThread.Post(() => info2.Text = ifo);
                                Thread.Sleep(100);
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