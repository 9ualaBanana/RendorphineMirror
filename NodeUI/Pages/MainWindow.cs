using System.Web;
using MonoTorrent;
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


            var tabs = new TabbedControl();
            tabs.Add(Localized.Tab.Dashboard, new DashboardTab());
            tabs.Add(Localized.Tab.Plugins, new PluginsTab());
            tabs.Add(Localized.Tab.Benchmark, new BenchmarkTab());
            tabs.Add(new("torrent test"), new TorrentTab());

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
                        var get = await client.GetAsync($"http://127.0.0.1:{Settings.LocalListenPort}/uploadtorrent?url={HttpUtility.UrlEncode(url)}&dir={HttpUtility.UrlEncode(dir)}").ConfigureAwait(false);
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
                            bool die = false;
                            while (true)
                            {
                                var info = await client.GetAsync($"http://{url}/torrentinfo?hash={HttpUtility.UrlEncode(hash.ToHex())}").ConfigureAwait(false);
                                var ifo = await info.Content.ReadAsStringAsync().ConfigureAwait(false);

                                try
                                {
                                    if (JObject.Parse(ifo)["progress"]?.Value<double>() > 99)
                                        Dispatcher.UIThread.Post(() => info2.Text = ifo + "\nUpload completed.");

                                    if (!die && JObject.Parse(ifo)["progress"]?.Value<double>() > 99)
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

                                Dispatcher.UIThread.Post(() => info2.Text = ifo);
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