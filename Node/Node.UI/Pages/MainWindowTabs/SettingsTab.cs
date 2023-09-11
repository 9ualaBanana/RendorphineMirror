using Newtonsoft.Json.Linq;

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
                    CreatePorts(),
                },
            },
        };
        Children.Add(scroll);
    }

    Control CreateNick()
    {
        var nicktb = new TextBox();
        NodeGlobalState.Instance.BNodeName.SubscribeChanged(() => Dispatcher.UIThread.Post(() => nicktb.Text = NodeGlobalState.Instance.NodeName), true);

        var nicksbtn = new MPButton() { Text = new("Set nickname"), };
        nicksbtn.OnClickSelf += async self =>
        {
            using var _ = new FuncDispose(() => Dispatcher.UIThread.Post(() => nicksbtn.IsEnabled = true));
            nicksbtn.IsEnabled = false;

            var nick = nicktb.Text.Trim();
            if (NodeGlobalState.Instance.NodeName == nick)
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
    Control CreatePorts()
    {
        var jsonpanel = new Panel();
        var setting = null as JsonUISetting.Setting;

        var json = new JObject();
        NodeGlobalState.Instance.AnyChanged.Subscribe(this, _ => Dispatcher.UIThread.Post(updatecontrol));
        updatecontrol();
        void updatecontrol()
        {
            var obj = new
            {
                port = NodeGlobalState.Instance.UPnpPort,
                webport = NodeGlobalState.Instance.UPnpServerPort,
                torrentport = NodeGlobalState.Instance.TorrentPort,
                dhtport = NodeGlobalState.Instance.DhtPort,
            };

            json = JObject.FromObject(obj);
            jsonpanel.Children.Clear();
            jsonpanel.Children.Add(setting = JsonEditorList.Default.Create(new JProperty("ae", json), FieldDescriber.Create(obj.GetType())));
        }


        return new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                jsonpanel,
                new MPButton()
                {
                    Text = new("Save ports"),
                    OnClickSelf = async self =>
                    {
                        setting!.UpdateValue();

                        var data = new[] { "port", "webport", "torrentport", "dhtport" }.Select(x => (x, json[x]!.Value<ushort>().ToString())).ToArray();
                        var update = await LocalApi.Default.Get("updateports", "Updating ports config", data);
                        await self.Flash(update);
                    },
                },
            },
        };
    }
}