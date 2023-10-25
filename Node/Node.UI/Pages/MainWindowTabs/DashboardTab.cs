using Avalonia.Controls.ApplicationLifetimes;

namespace Node.UI.Pages.MainWindowTabs;

public class DashboardTab : Panel
{
    public DashboardTab(NodeGlobalState state)
    {
        var starttime = DateTimeOffset.Now;
        var infotb = new TextBlock()
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        var configtb = new TextBlock()
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
            Dispatcher.UIThread.Post(() =>
            {
                infotb.Text = $"""
                    Authenticated as {JsonConvert.SerializeObject(state.AuthInfo.Value ?? default, Formatting.None)}
                    Balance: {state.Balance.Value.Balance.ToString(CultureInfo.InvariantCulture)} EUR
                    Completed tasks since {state.CompletedTasks.MinBy(t => t.FinishTime)?.FinishTime.ToString(CultureInfo.InstalledUICulture) ?? "never"}:
                    {string.Join(Environment.NewLine, state.CompletedTasks.GroupBy(t => t.TaskInfo.FirstAction).Select(t => $"    {t.Key}: {t.Count()}"))}
                    """;

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
    }
}
