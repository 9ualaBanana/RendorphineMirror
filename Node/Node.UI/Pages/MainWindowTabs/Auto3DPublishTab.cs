namespace Node.UI.Pages.MainWindowTabs;

public class Auto3DPublishTab : Panel
{
    readonly BindableDictionary<string, AutoRFProductPublishInfo> Infos;

    public Auto3DPublishTab(NodeGlobalState state)
    {
        Infos = state.AutoRFProductPublishInfos.GetBoundCopy();

        var parts = new Dictionary<string, Part>();

        var stack = new StackPanel() { Orientation = Orientation.Vertical };
        Children.Add(stack);

        Infos.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
        {
            var infos = Infos.Value;

            foreach (var (key, part) in parts)
            {
                if (!infos.ContainsKey(key))
                {
                    parts.Clear();
                    stack.Children.Clear();
                    break;
                }
            }

            foreach (var (key, info) in infos.ToArray())
            {
                if (!parts.TryGetValue(key, out var part))
                    stack.Children.Add((parts[key] = part = new Part()).Named(key));

                part.SetInfo(info);
            }
        }, DispatcherPriority.Background), true);
    }


    class Part : StackPanel
    {
        readonly TextBlock InputDir;
        readonly TextBlock State;
        readonly TextBlock Files;
        readonly TextBlock RFProducted;
        readonly TextBlock Published;
        readonly TextBlock Error;
        string? TaskId;

        public Part()
        {
            Orientation = Orientation.Vertical;
            Children.Clear();
            Children.AddRange([
                InputDir = new TextBlock(),
                State = new TextBlock(),
                Files = new TextBlock(),
                RFProducted = new TextBlock(),
                Published = new TextBlock(),
                new MPButton()
                {
                    Text = "FULL RESTART",
                    OnClickSelf = async self =>
                    {
                        if (TaskId is null) return;

                        await new YesNoMessageBox(false)
                        {
                            Text = "Are you sure you want to do a full restart ???",
                            OnClick = async doit =>
                            {
                                if (!doit) return;

                                var result = await LocalApi.Default.Post("auto3drestart", "Restarting the auto 3d generator", ("taskid", TaskId));
                                await self.Flash(result);
                                State.Text = "waiting for timer (10sec)";
                            }
                        }.ShowDialog((Window) VisualRoot!);
                    },
                },
                Error = new TextBlock() { Foreground = Colors.From(255, 0, 0), TextWrapping = TextWrapping.Wrap },
            ]);
        }

        public void SetInfo(AutoRFProductPublishInfo info)
        {
            TaskId = info.TaskId;
            InputDir.Text = $"Input dir: {info.InputDirectory}";

            State.Text =
                info.CurrentRFProducting is not null ? $"Creating RFProduct of {info.CurrentRFProducting}"
                : info.CurrentPublishing is not null ? $"Publishing {info.CurrentPublishing}"
                : "Idle";

            Files.Text = $"Files: {info.FileCount}";
            RFProducted.Text = $"RFProducted: {info.RFProductedCount?.ToString() ?? "?"}";
            Published.Text = $"Published: {info.PublishedCount?.ToString() ?? "?"}";
            Error.Text = info.Error;
        }
    }
}
