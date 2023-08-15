using Avalonia.Controls.ApplicationLifetimes;

namespace Node.UI.Pages.MainWindowTabs;

public class DashboardTab : Panel
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
        updatetext();
        NodeGlobalState.Instance.AnyChanged.Subscribe(this, _ => updatetext());
        NodeGlobalState.Instance.BenchmarkResult.SubscribeChanged(updatetext);


        var baselist = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                NamedControl.Create("Info", infotb),
                NamedControl.Create("Buttons", new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        NodeGlobalState.Instance.AuthInfo?.Slave != false
                            ? new Panel()
                            : new MPButton()
                            {
                                Text = new("Create a task"),
                                OnClick = () => new TaskCreationWindow().Show(),
                            },
                        new MPButton()
                        {
                            Text = "lang.current",
                            OnClick = () => UISettings.Language = UISettings.Language == "ru-RU" ? "en-US" : "ru-RU",
                        }.With(btn => LocalizedString.ChangeLangWeakEvent.Subscribe(btn, () => btn.Text = "Language: " + new LocalizedString("lang.current"))),
                        new MPButton()
                        {
                            Text = new("Log out"),
                            OnClickSelf = async self =>
                            {
                                var logout = await LocalApi.Default.Get("logout", "Logging out");
                                if (await self.FlashErrorIfErr(logout))
                                    return;

                                ((IClassicDesktopStyleApplicationLifetime) Application.Current!.ApplicationLifetime!).MainWindow = new LoginWindow();
                                ((Window) VisualRoot!).Close();
                            },
                        },
                    },
                }),
            },
        };
        Children.Add(baselist);


        void updatetext()
        {
            Dispatcher.UIThread.Post(() => infotb.Text =
                @$"
                Ui start time: {starttime}

                Auth: {JsonConvert.SerializeObject(NodeGlobalState.Instance.AuthInfo ?? default, Formatting.None)}
                Ports: {JsonConvert.SerializeObject(new { NodeGlobalState.Instance.LocalListenPort, NodeGlobalState.Instance.UPnpPort, NodeGlobalState.Instance.UPnpServerPort, NodeGlobalState.Instance.DhtPort, NodeGlobalState.Instance.TorrentPort })}
                Benchmark: {NodeGlobalState.Instance.BenchmarkResult.Value?.ToString(Formatting.None) ?? "bench mark"}
                ".TrimLines()
            );
        }
    }
}
