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
        readonly TextBlock RFProductsDir;
        readonly TextBlock State;
        readonly TextBlock Files;
        readonly TextBlock RFProducted;
        readonly TextBlock Published;

        public Part()
        {
            Orientation = Orientation.Vertical;
            Children.Clear();
            Children.AddRange([
                InputDir = new TextBlock(),
                RFProductsDir = new TextBlock(),
                State = new TextBlock(),
                Files = new TextBlock(),
                RFProducted = new TextBlock(),
                Published = new TextBlock(),
            ]);
        }

        public void SetInfo(AutoRFProductPublishInfo info)
        {
            InputDir.Text = $"Input dir: {info.InputDirectory}";
            RFProductsDir.Text = $"RFProduct dir: {info.RFProductDirectory}";

            State.Text =
                info.CurrentRFProducting is not null ? $"Creating RFProduct of {info.CurrentRFProducting}"
                : info.CurrentPublishing is not null ? $"Publishing {info.CurrentPublishing}"
                : "Idle";

            Files.Text = $"Files: {info.FileCount}";
            RFProducted.Text = $"RFProducted: {info.RFProductedCount?.ToString() ?? "?"}";
            Published.Text = $"Published: {info.PublishedCount?.ToString() ?? "?"}";
        }
    }
}
