using System.Net;
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

                var iptb = new TextBox() { Text = "ip:port" };
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
                        iptb.WithRow(2),
                        dirtb.WithRow(3),
                        button.WithRow(4),
                    },
                };
                Children.Add(torrentgrid);


                async void click()
                {
                    var ip = iptb.Text.Trim();
                    var dir = dirtb.Text.Trim();

                    if (!Directory.Exists(dir))
                    {
                        button.Text = new("err dir not found");
                        return;
                    }
                    if (!IPEndPoint.TryParse(ip, out _))
                    {
                        button.Text = new("err invalid url");
                        return;
                    }


                    var peerid = TorrentClient.PeerId.UrlEncode();
                    var peerurl = PortForwarding.GetPublicIPAsync().ConfigureAwait(false);
                    var (data, manager) = await TorrentClient.CreateAddTorrent(dir).ConfigureAwait(false);

                    try
                    {
                        var postresponse = await new HttpClient().PostAsync($"http://{ip}/downloadtorrent?peerid={peerid}&peerurl={await peerurl}:{TorrentClient.ListenPort}", new ByteArrayContent(data)).ConfigureAwait(false);
                        if (!postresponse.IsSuccessStatusCode)
                        {
                            await TorrentClient.Client.RemoveAsync(manager).ConfigureAwait(false);
                            button.Text = new("err " + postresponse.StatusCode + " " + await postresponse.Content.ReadAsStringAsync().ConfigureAwait(false));

                            return;
                        }

                        new Thread(() =>
                        {
                            while (true)
                            {
                                var ps = string.Join("\n", manager.GetPeersAsync().Result.Select(x => $"peer {x.Uri}: uploaded {x.Monitor.DataBytesUploaded / 1024}kb  speed {x.Monitor.UploadSpeed / 1024}kbs"));
                                var ifo = $"leech: {manager.Peers.Leechs}\n{ps}";

                                Dispatcher.UIThread.Post(() => info2.Text = ifo);
                                Thread.Sleep(100);
                            }
                        })
                        { IsBackground = true }.Start();
                    }
                    catch { Dispatcher.UIThread.Post(() => button.Text = new("connection error")); }
                }
            }
        }
    }
}