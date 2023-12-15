using Avalonia.Controls.ApplicationLifetimes;

namespace Node.UI.Pages.MainWindowTabs;

public class DashboardTab : Panel
{
    public DashboardTab(NodeGlobalState state)
    {
        var starttime = DateTimeOffset.Now;
        var infotb = new SelectableTextBlock()
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        var webserveruritb1 = new MPButton()
        {
            VerticalAlignment = VerticalAlignment.Center,
            Background = Colors.AlmostTransparent,
            Foreground = Colors.From(100, 100, 0),
            OnClickSelf = (self) => Process.Start(new ProcessStartInfo(self.Text.ToString()) { UseShellExecute = true }),
        };
        var webserveruritb2 = new MPButton()
        {
            VerticalAlignment = VerticalAlignment.Center,
            Background = Colors.AlmostTransparent,
            Foreground = Colors.From(100, 100, 0),
            OnClickSelf = (self) => Process.Start(new ProcessStartInfo(self.Text.ToString()) { UseShellExecute = true }),
        };

        var configtb = new SelectableTextBlock()
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };

        updatetext();
        state.AnyChanged.Subscribe(this, _ => updatetext());


        var baselist = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                NamedControl.Create("Info", infotb)
                    .With(c => c.Title.Bind(state.NodeName)),
                NamedControl.Create("Web server", new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        webserveruritb1,
                        webserveruritb2,
                    },
                }),
                NamedControl.Create("Info", configtb),
                NamedControl.Create("Buttons", new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        state.AuthInfo.Value?.Slave != false
                            ? new Panel()
                            : new MPButton()
                            {
                                Text = new("Create a task"),
                                OnClick = () => new TaskCreationWindow().Show(),
                            },
                        new MPButton()
                        {
                            Text = "lang.current",
                            OnClick = () => App.Current.Settings.Language = App.Current.Settings.Language == "ru-RU" ? "en-US" : "ru-RU",
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

        // Earned ~{earnedFromTasks.ToString(CultureInfo.InvariantCulture)} EUR from completing {completedTasks.Count} tasks since {lastTaskUpdate}
        void updatetext()
        {
            try
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    infotb.Text = $"""
                        Authenticated as {JsonConvert.SerializeObject(state.AuthInfo.Value ?? default, Formatting.None)}
                        Balance: {state.Balance.Value.Balance.ToString(CultureInfo.InvariantCulture)} EUR
                        Completed tasks since {state.CompletedTasks.MinBy(t => t.FinishTime)?.FinishTime.ToString(CultureInfo.InstalledUICulture) ?? "never"}:
                        {string.Join(Environment.NewLine, state.CompletedTasks.GroupBy(t => t.TaskInfo.FirstAction).Select(t => $"    {t.Key}: {t.Count()}"))}
                        """;

                    webserveruritb1.Text = $"http://{(await PortForwarding.GetPublicIPAsync())}:{state.UPnpServerPort.Value}";
                    webserveruritb2.Text = $"http://127.0.0.1:{state.UPnpServerPort.Value}";

                    configtb.Text = $"""
                        Ui start time: {starttime}

                        Ports: {JsonConvert.SerializeObject(new
                    {
                        LocalListenPort = state.LocalListenPort.Value,
                        UPnpPort = state.UPnpPort.Value,
                        UPnpServerPort = state.UPnpServerPort.Value,
                        DhtPort = state.DhtPort.Value,
                        TorrentPort = state.TorrentPort.Value,
                    })}
                        Benchmark: {state.BenchmarkResult.Value?.ToString(Formatting.None) ?? "not completed yet"}
                        """;
                });
            }
            catch { }
        }
    }
}
