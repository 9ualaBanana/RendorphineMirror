namespace Node.UI.Pages.MainWindowTabs;

public class SettingsTab : Panel
{
    public SettingsTab()
    {
        var scroll = new ScrollViewer()
        {
            Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 40,
                Children =
                {
                    CreateNick(),
                    CreateOthers(),
                    CreateTasksDir(),
                },
            },
        };
        Children.Add(scroll);
    }

    Control CreateOthers()
    {
        var receiveTasksCb = new CheckBox() { Content = new LocalizedString("Receive tasks") };
        NodeGlobalState.Instance.AcceptTasks.SubscribeChanged(() => Dispatcher.UIThread.Post(() => receiveTasksCb.IsChecked = NodeGlobalState.Instance.AcceptTasks.Value), true);

        var prevReceiveTasksCb = NodeGlobalState.Instance.AcceptTasks.Value;
        receiveTasksCb.IsCheckedChanged += (obj, e) =>
        {
            var c = receiveTasksCb.IsChecked == true;
            if (c == prevReceiveTasksCb)
                return;
            prevReceiveTasksCb = c;

            Task.Run(async () =>
            {
                var set = await LocalApi.Default.Get("setaccepttasks", "Setting Accept Tasks", ("accept", JsonConvert.SerializeObject(c)));
            }).Consume();
        };

        var processTasksCb = new CheckBox() { Content = new LocalizedString("Process tasks") };
        NodeGlobalState.Instance.ProcessTasks.SubscribeChanged(() => Dispatcher.UIThread.Post(() => receiveTasksCb.IsChecked = NodeGlobalState.Instance.ProcessTasks.Value), true);
        var prevProcessTasksCb = NodeGlobalState.Instance.ProcessTasks.Value;
        processTasksCb.IsCheckedChanged += (obj, e) =>
        {
            var c = processTasksCb.IsChecked == true;
            if (c == prevProcessTasksCb)
                return;
            prevProcessTasksCb = c;

            Task.Run(async () =>
            {
                var set = await LocalApi.Default.Get("setprocesstasks", "Setting Process Tasks", ("process", JsonConvert.SerializeObject(c)));
            }).Consume();
        };


        return new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("* *"),
            Children =
            {
                receiveTasksCb.WithRow(0),
                processTasksCb.WithRow(1),
            }
        };
    }
    Control CreateNick()
    {
        var nicktb = new TextBox();
        NodeGlobalState.Instance.NodeName.SubscribeChanged(() => Dispatcher.UIThread.Post(() => nicktb.Text = NodeGlobalState.Instance.NodeName.Value), true);

        var nicksbtn = new MPButton() { Text = new("Set nickname"), };
        nicksbtn.OnClickSelf += async self =>
        {
            if (string.IsNullOrWhiteSpace(nicktb.Text))
            {
                await self.FlashError("No nickname");
                return;
            }

            using var _ = new FuncDispose(() => Dispatcher.UIThread.Post(() => nicksbtn.IsEnabled = true));
            nicksbtn.IsEnabled = false;

            var nick = nicktb.Text.Trim();
            if (NodeGlobalState.Instance.NodeName.Value == nick)
            {
                await self.FlashError("Can't change nickname to the same one");
                return;
            }

            var set = await LocalApi.Default.Get("setnick", "Changing node nickname", ("nick", nick));
            await self.Flash(set);
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
    Control CreateTasksDir()
    {
        var tb = new TextBox();
        NodeGlobalState.Instance.TaskProcessingDirectory.SubscribeChanged(() => Dispatcher.UIThread.Post(() => tb.Text = NodeGlobalState.Instance.TaskProcessingDirectory.Value), true);

        var panel = new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("* Auto"),
            Children =
            {
                tb.WithColumn(0),
                new MPButton()
                {
                    Text = "Choose directory",
                    OnClick = async () =>
                    {
                        var result = await OpenDirectoryPicker();
                        tb.Text = result;
                    },
                }.WithColumn(1),
            },
        };

        async Task<string> OpenDirectoryPicker()
        {
            var result = await ((Window) VisualRoot!).StorageProvider.OpenFolderPickerAsync(new() { AllowMultiple = false });
            return result.FirstOrDefault()?.Path.LocalPath ?? string.Empty;
        }

        var setbtn = new MPButton() { Text = new("Set task directory"), };
        setbtn.OnClickSelf += async self =>
        {
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                await self.FlashError("No directory");
                return;
            }



            using var _ = new FuncDispose(() => Dispatcher.UIThread.Post(() => setbtn.IsEnabled = true));
            setbtn.IsEnabled = false;

            var dir = tb.Text.Trim();
            var set = await LocalApi.Default.Get("settaskdir", "Changing task processing directory", ("dir", dir));
            await self.Flash(set);
        };

        return new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("* *"),
            Children =
            {
                panel.WithRow(0),
                setbtn.WithRow(1),
            }
        };
    }
}
