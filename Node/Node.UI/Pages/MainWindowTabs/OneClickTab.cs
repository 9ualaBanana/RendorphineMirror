namespace Node.UI.Pages.MainWindowTabs;

public class OneClickTab : Panel
{
    readonly NodeGlobalState NodeGlobalState;

    public OneClickTab(NodeGlobalState nodeGlobalState)
    {
        NodeGlobalState = nodeGlobalState;
        NodeGlobalState.OneClickTaskInfo.SubscribeChanged(() => Dispatcher.UIThread.Post(Recreate), true);
    }

    void Recreate()
    {
        Children.Clear();

        var task = NodeGlobalState.OneClickTaskInfo.Value;
        if (task is null)
        {
            Children.Add(new TextBlock() { Text = "OneClick wtask is not launched." });
            return;
        }

        Panel panelContent;
        if (task is null)
        {
            panelContent = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new TextBlock()
                    {
                        Text = "OneClick task is not created",
                    },
                    /*
                    new MPButton()
                    {
                        Text = "Create",
                        OnClickSelf = async self =>
                        {

                        },
                    },
                    */
                },
            };
        }
        else
        {
            panelContent = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Children =
                {
                    new ToggleSwitch()
                    {
                        Content = "Processing",
                        OnContent = "ON",
                        OffContent = "OFF",
                        IsChecked = !task.IsPaused,
                    }.With(s =>
                    {
                        s.IsCheckedChanged += async (obj, e) =>
                        {
                            await LocalApi.Default.Get($"oc/{(s.IsChecked == true ? "unpause" : "pause")}", $"{(s.IsChecked == true ? "Unpausing" : "Pausing")} oneclick");
                        };
                    }),
                    new TextBlock()
                    {
                        Text = $"""
                            Input dir: {task.InputDir}
                            Output dir: {task.OutputDir}
                            Log dir: {task.LogDir}
                            Unity templates dir: {task.UnityTemplatesDir}
                            """,
                    }.Named("Info"),
                    new TextBlock().With(tb =>
                    {
                        var all = task.ExportInfo.Count;
                        var oneclickcompleted = task.ExportInfo.Values.Count(r => r.OneClick is { Successful: true });
                        var unityfullcompleted = task.ExportInfo.Values.Count(r => r.Unity?.All(u => u.Value.Successful) == true);

                        tb.Text = $"""
                            Input archives: {all}
                            3dsmax completed: {oneclickcompleted}
                            unity completed: {unityfullcompleted}
                            """;
                    })
                    .Named("Stats"),
                    new TextBlock()
                    {
                        Text = $"""
                            Exports:
                            {string.Join('\n', task.ExportInfo?.Select(e => $"""
                                {e.Key}:
                                    3dsMax: {(e.Value.OneClick is null ? "Not processed" : e.Value.OneClick.Successful ? e.Value.OneClick.Version : "Not successful")}
                                    Unity: {(e.Value.Unity?.Count is null or 0 ? "<not completed>" : null)}\n        {string.Join('\n', e.Value.Unity?.Select(u => $"        {u.Key}: {u.Value.ImporterVersion}") ?? [])}
                                """.TrimEnd()) ?? [])}
                            """,
                    }.Named("Processing"),
                },
            };
        }

        Children.Add(new ScrollViewer() { Content = panelContent });
    }
}

