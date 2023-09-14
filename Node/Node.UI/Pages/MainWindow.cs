using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Node.UI.Pages.MainWindowTabs;
using NodeToUI.Requests;

namespace Node.UI.Pages
{
    public partial class MainWindow : Window
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public MainWindow(NodeStateUpdater nodeStateUpdater)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            Width = 692;
            Height = 410;
            Title = App.Instance.AppName;
            Icon = App.Instance.Icon;

            this.PreventClosing();
            SubscribeToStateChanges();


            var tabs = new TabbedControl();
            tabs.Add("tasks", new TasksTab2());
            tabs.Add("tab.dashboard", new DashboardTab(NodeGlobalState.Instance));
            tabs.Add("tab.plugins", new PluginsTab());
            tabs.Add("menu.settings", new SettingsTab());
            tabs.Add("logs", new LogsTab());
            if (App.Instance.Init.DebugFeatures) tabs.Add("registry", new JsonRegistryTab());
            tabs.Add("3dupload", new Model3DUploadTab());

            var statustb = new TextBlock()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.Bold,
            };

            Content = new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto *"),
                Children =
                {
                    statustb.WithRow(0),
                    tabs.WithRow(1),
                },
            };


            nodeStateUpdater.IsConnectedToNode.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
            {
                if (nodeStateUpdater.IsConnectedToNode.Value) statustb.Text = null;
                else
                {
                    statustb.Text = "!!! No connection to node !!!";
                    statustb.Foreground = Brushes.Red;
                }
            }), true);
        }

        void SubscribeToStateChanges()
        {
            IMessageBox? benchmb = null;
            NodeGlobalState.Instance.ExecutingBenchmarks.Changed += () => Dispatcher.UIThread.Post(() =>
            {
                var benchs = NodeGlobalState.Instance.ExecutingBenchmarks;

                if (benchs.Count != 0)
                {
                    if (benchmb is null)
                    {
                        benchmb = new MessageBox(new("Hide"));
                        benchmb.Show();
                    }

                    benchmb.Text = new(@$"
                        Benchmarking your system...
                        {benchs.Count} completed: {JsonConvert.SerializeObject(benchs)}
                    ".TrimLines());
                }
                else
                {
                    benchmb?.Close();
                    benchmb = null;
                }
            });


            var receivedrequests = new Dictionary<string, GuiRequest>();
            NodeGlobalState.Instance.Requests.Changed += () => Dispatcher.UIThread.Post(() =>
            {
                var requests = NodeGlobalState.Instance.Requests.Value;
                foreach (var req in receivedrequests.ToArray())
                {
                    if (requests.ContainsKey(req.Key)) continue;

                    receivedrequests.Remove(req.Key);
                    req.Value.OnRemoved();
                }
                foreach (var req in requests)
                {
                    if (receivedrequests.ContainsKey(req.Key)) continue;

                    // added
                    receivedrequests.Add(req.Key, req.Value);
                    handle(req.Key, req.Value);
                }



                void handle(string reqid, GuiRequest request)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        GuiRequestWindow window = request switch
                        {
                            CaptchaRequest req => new CaptchaWindow(req.Base64Image, v => sendResponse(v)),
                            InputRequest req => new InputWindow(req.Text, v => sendResponse(v)),
                            InputTurboSquidModelInfoRequest req => new TurboSquidModelInfoInputWindow(req, v => sendResponse(v)),
                            _ => throw new InvalidOperationException("Unknown request type " + request),
                        };

                        request.OnRemoved = () => Dispatcher.UIThread.Post(() => { try { window.ForceClose(); } catch { } });
                        window.Show();
                    });


                    async Task sendResponse(JToken token)
                    {
                        token = new JObject() { ["value"] = token };

                        using var content = new StringContent(token.ToString());
                        var reqtype = NodeGui.GuiRequestNames[request.GetType()];
                        var post = await LocalApi.Default.Post($"{reqtype}?reqid={HttpUtility.UrlEncode(reqid)}", $"Sending {reqtype} request", content);
                        post.LogIfError();

                        receivedrequests.Remove(reqid);
                    }
                }
            });
        }
    }
}