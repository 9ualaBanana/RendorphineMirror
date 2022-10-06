using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Common.Tasks.Model;
using MonoTorrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeUI.Pages
{
    public class MainWindow : Window
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            Width = 692;
            Height = 410;
            Title = App.AppName;
            Icon = App.Icon;

            this.PreventClosing();
            SubscribeToStateChanges();


            var tabs = new TabbedControl();
            tabs.Add("tasks2", new TasksTab2());
            tabs.Add("tab.dashboard", new DashboardTab());
            tabs.Add("tab.plugins", new PluginsTab());
            tabs.Add("tasks", new TasksTab());
            tabs.Add("tab.benchmark", new BenchmarkTab());
            tabs.Add("menu.settings", new SettingsTab());
            tabs.Add("torrent test", new TorrentTab());
            tabs.Add("logs", new LogsTab());
            if (Init.IsDebug)
                tabs.Add("registry", new RegistryTab());

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


            UICache.IsConnectedToNode.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
            {
                if (UICache.IsConnectedToNode.Value) statustb.Text = null;
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
        }


        static class NamedList
        {
            public static NamedList<T> Create<T>(string title, IReadOnlyCollection<T> items, Func<T, IControl> templatefunc) => new(title, items, templatefunc);
        }
        class NamedControl : Panel
        {
            protected readonly string TitleText;
            protected readonly TextBlock Title;
            public readonly Panel Control;

            public NamedControl(string title)
            {
                TitleText = title;
                Title = new TextBlock();
                Control = new Panel();

                UpdateTitle();


                Children.Add(new Grid()
                {
                    RowDefinitions = RowDefinitions.Parse("Auto *"),
                    Children =
                    {
                        Title.WithRow(0),
                        Control.WithRow(1),
                    },
                });
            }

            public void UpdateTitle() => Dispatcher.UIThread.Post(() => Title.Text = $"{TitleText}\nLast update: {DateTimeOffset.Now}");
        }
        class NamedList<T> : NamedControl
        {
            // GC protected instance
            readonly IReadOnlyCollection<T> Items;

            public NamedList(string title, IReadOnlyCollection<T> items, Func<T, IControl> templatefunc) : base(title)
            {
                Items = items = (items as IReadOnlyBindableCollection<T>)?.GetBoundCopy() ?? items;

                (items as IReadOnlyBindableCollection<T>)?.SubscribeChanged(UpdateTitle, true);
                Control.Children.Add(TypedItemsControl.Create(items, templatefunc));
            }
        }

        class DashboardTab : Panel
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
                Children.Add(infotb);
                updatetext();
                Settings.AnyChanged += updatetext;
                NodeGlobalState.Instance.AnyChanged.Subscribe(this, _ => updatetext());

                var langbtn = new MPButton()
                {
                    MaxWidth = 100,
                    MaxHeight = 30,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Text = "lang.current",
                    OnClick = () => UISettings.Language = UISettings.Language == "ru-RU" ? "en-US" : "ru-RU",
                };
                Children.Add(langbtn);

                var unloginbtn = new MPButton()
                {
                    MaxWidth = 100,
                    MaxHeight = 30,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(200, 0, 0, 0),
                    Text = new("unlogin"),
                    OnClick = () =>
                    {
                        Settings.AuthInfo = null;
                        Settings.NodeName = null!;

                        LocalApi.Send("reloadcfg").AsTask().Consume();
                        new LoginWindow().Show();
                        ((Window) VisualRoot!).Close();
                    },
                };
                Children.Add(unloginbtn);

                if (Settings.IsSlave == false)
                {
                    var taskbtn = new MPButton()
                    {
                        Text = new("new task"),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        OnClick = () => new TaskCreationWindow().Show(),
                    };
                    Children.Add(taskbtn);
                }


                void updatetext()
                {
                    Dispatcher.UIThread.Post(() => infotb.Text =
                        @$"
                        Auth: {JsonConvert.SerializeObject(Settings.AuthInfo!.Value, Formatting.None)}
                        Ports: [ LocalListen: {Settings.LocalListenPort}; UPnp: {Settings.UPnpPort}; UPnpServer: {Settings.UPnpServerPort}; Dht: {Settings.DhtPort}; Torrent: {Settings.TorrentPort} ]

                        Ui start time: {starttime}
                        ".TrimLines()
                    );
                }
            }
        }
        class PluginsTab : Panel
        {
            public PluginsTab()
            {
                var scroll = new ScrollViewer()
                {
                    Content = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 20,
                        Children =
                        {
                            new InstallPluginPanel(),
                            new Panel() { Background = Colors.Black, Width = 400, },
                            NamedList.Create("Software stats", UICache.SoftwareStats, softToControl),
                            NamedList.Create("Our plugins", NodeGlobalState.Instance.InstalledPlugins, pluginToControl),
                        },
                    },
                };

                Children.Add(scroll);


                IControl softToControl(KeyValuePair<PluginType, SoftwareStats> value)
                {
                    var (type, stat) = value;

                    return new Expander()
                    {
                        Header = $"{type} ({stat.Total} total installs; {stat.ByVersion.Count} different versions; {stat.ByVersion.Sum(x => (long) x.Value.Total)} total installed versions)",
                        Content = new ItemsControl()
                        {
                            Items = stat.ByVersion.OrderByDescending(x => x.Value.Total).Select(v => $"{v.Key} ({v.Value.Total})"),
                        },
                    };
                }
                IControl pluginToControl(Plugin plugin) => new TextBlock() { Text = $"{plugin.Type} {plugin.Version}: {plugin.Path}" };
            }


            class InstallPluginPanel : Panel
            {
                readonly object Bindable;

                public InstallPluginPanel()
                {
                    var versionslist = TypedComboBox.Create(Array.Empty<string>());
                    versionslist.SelectedIndex = 0;

                    var pluginslist = TypedComboBox.Create(Array.Empty<string>());
                    pluginslist.SelectionChanged += (obj, e) => versionslist.Items = UICache.SoftwareStats[Enum.Parse<PluginType>(pluginslist.SelectedItem ?? PluginType.FFmpeg.ToString())].ByVersion.Select(x => x.Key).ToArray();
                    pluginslist.SelectedIndex = 0;

                    var cp = UICache.SoftwareStats.GetBoundCopy();
                    cp.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
                    {
                        pluginslist.Items = UICache.SoftwareStats.Select(x => x.Key.ToString()).ToArray();
                        pluginslist.SelectedIndex = pluginslist.SelectedIndex;
                    }), true);
                    Bindable = cp;

                    var installbtn = new MPButton()
                    {
                        Text = "Install plugin",
                        OnClickSelf = async self =>
                        {
                            var res = await LocalApi.Send($"deploy?type={HttpUtility.UrlEncode(pluginslist.SelectedItem)}&version={HttpUtility.UrlEncode(versionslist.SelectedItem)}");
                            if (!res) await self.TemporarySetText("err " + res.AsString());
                        },
                    };

                    var panel = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children = { pluginslist, versionslist, installbtn },
                    };
                    Children.Add(panel);
                }
            }
        }
        class TasksTab : Panel
        {
            public TasksTab()
            {
                var allplaced = null as StackPanel;
                allplaced = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        new MPButton()
                        {
                            Text = "Fetch all active placed tasks",
                            OnClickSelf = async self =>
                            {
                                allplaced.ThrowIfNull();

                                var tasks = await Apis.GetMyTasksAsync(new[] { TaskState.Queued, TaskState.Input, TaskState.Active, TaskState.Output, });
                                await self.TemporarySetTextIfErr(tasks);
                                if (!tasks) return;

                                if (allplaced.Children.Count > 1)
                                    allplaced.Children.RemoveAt(1);
                                allplaced.Children.Add(NamedList.Create("ALL active placed tasks", tasks.ThrowIfError(), placedTasksCreate));
                            },
                        },
                    },
                };


                var scroll = new ScrollViewer()
                {
                    Content = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 20,
                        Children =
                        {
                            NamedList.Create("Executing tasks", NodeGlobalState.Instance.ExecutingTasks, execTasksCreate),
                            NamedList.Create("Watching tasks", NodeGlobalState.Instance.WatchingTasks, watchingTasksCreate),
                            NamedList.Create("Placed tasks", NodeGlobalState.Instance.PlacedTasks, placedTasksCreate),
                            allplaced,
                        },
                    },
                };

                Children.Add(scroll);


                IControl execTasksCreate(ReceivedTask task)
                {
                    var statustb = new TextBlock();

                    return new Expander()
                    {
                        Header = $"{task.Id} {task.GetPlugin()} {task.Action}",
                        Content = new StackPanel()
                        {
                            Orientation = Orientation.Vertical,
                            Children =
                            {
                                new TextBlock() { Text = $"Data: {task.Info.Data.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Input: {JsonConvert.SerializeObject( task.Info.Input,Formatting.None)}" },
                                new TextBlock() { Text = $"Output: {JsonConvert.SerializeObject( task.Info.Output,Formatting.None)}" },
                                statustb,
                            },
                        },
                    };
                }
                IControl placedTasksCreate(DbTaskFullState task)
                {
                    var statustb = new TextBlock();
                    var statusbtn = new MPButton()
                    {
                        Text = "Update status",
                        OnClickSelf = async self => await updateState(self),
                    };
                    var cancelbtn = new MPButton()
                    {
                        Text = "Cancel task",
                        OnClickSelf = async self =>
                        {
                            var cstate = await task.ChangeStateAsync(TaskState.Canceled);
                            await self.TemporarySetTextIfErr(cstate);
                            if (!cstate) return;

                            await updateState(self);
                        },
                    };

                    async Task updateState(MPButton button)
                    {
                        var state = await task.GetTaskStateAsync();
                        await button.TemporarySetTextIfErr(state);
                        if (!state) return;

                        statustb.Text = JsonConvert.SerializeObject(state.Value, Formatting.None);
                    }

                    return new Expander()
                    {
                        Header = $"{task.Id} {task.GetPlugin()} {task.Action}",
                        Content = new StackPanel()
                        {
                            Orientation = Orientation.Vertical,
                            Children =
                            {
                                new TextBlock() { Text = $"Data: {task.Info.Data.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Input: {JsonConvert.SerializeObject(task.Info.Input, Formatting.None)}" },
                                new TextBlock() { Text = $"Output: {JsonConvert.SerializeObject(task.Info.Output, Formatting.None)}" },
                                statustb,
                                new StackPanel()
                                {
                                    Orientation = Orientation.Horizontal,
                                    Children =
                                    {
                                        statusbtn,
                                        cancelbtn,
                                    },
                                },
                            },
                        },
                    };
                }
                IControl watchingTasksCreate(WatchingTaskInfo task)
                {
                    return new Expander()
                    {
                        Header = $"{task.Id} {NodeGlobalState.Instance.GetPluginTypeFromAction(task.TaskAction)} {task.TaskAction}",
                        Content = new StackPanel()
                        {
                            Orientation = Orientation.Vertical,
                            Children =
                            {
                                new TextBlock() { Text = $"Data: {task.TaskData.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Source: {task.Source.ToString(Formatting.None)}" },
                                new TextBlock() { Text = $"Output: {task.Output.ToString(Formatting.None)}" },
                            },
                        },
                    };
                }
            }
        }

        class TasksTab2 : Panel
        {
            string SessionId = Settings.SessionId;

            public TasksTab2()
            {
                var tabs = new TabbedControl();
                tabs.Add("Local", new LocalTaskManager());
                tabs.Add("Watching", new WatchingTaskManager());
                tabs.Add("Remote", new RemoteTaskManager());

                Children.Add(tabs);
            }


            abstract class TaskManager<T> : Panel
            {
                protected string SessionId = Settings.SessionId;

                public TaskManager()
                {
                    var data = CreateDataGrid();
                    Children.Add(WrapGrid(data));

                    LoadSetItems(data).Consume();
                }

                protected DataGrid CreateDataGrid()
                {
                    var data = new DataGrid() { AutoGenerateColumns = false };
                    data.BeginningEdit += (obj, e) => e.Cancel = true;

                    CreateColumns(data);
                    return data;
                }
                protected virtual Control WrapGrid(DataGrid grid)
                {
                    return new Grid()
                    {
                        RowDefinitions = RowDefinitions.Parse("Auto *"),
                        Children =
                        {
                            new MPButton()
                            {
                                Text = "Reload",
                                OnClick = () => { grid.Items = Array.Empty<T>(); LoadSetItems(grid).Consume(); },
                            }.WithRow(0),
                            grid.WithRow(1),
                        },
                    };
                }
                protected abstract void CreateColumns(DataGrid data);

                protected async Task LoadSetItems(DataGrid grid) => grid.Items = await Load();
                protected abstract Task<IReadOnlyCollection<T>> Load();
            }
            abstract class NormalTaskManager : TaskManager<ReceivedTask>
            {
                protected override void CreateColumns(DataGrid data)
                {
                    data.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding(nameof(ReceivedTask.Id)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "State", Binding = new Binding(nameof(ReceivedTask.State)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Action", Binding = new Binding(nameof(ReceivedTask.Action)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Input", Binding = new Binding("Input.Type") });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Output", Binding = new Binding("Output.Type") });

                    data.Columns.Add(new DataGridTextColumn() { Header = "Server Host", Binding = new Binding($"{nameof(DbTaskFullState.Server)}.{nameof(TaskServer.Host)}") });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Server Userid", Binding = new Binding($"{nameof(DbTaskFullState.Server)}.{nameof(TaskServer.Userid)}") });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Server Nickname", Binding = new Binding($"{nameof(DbTaskFullState.Server)}.{nameof(TaskServer.Nickname)}") });

                    data.Columns.Add(new DataGridButtonColumn<DbTaskFullState>()
                    {
                        Header = "Cancel task",
                        Text = "Cancel task",
                        CreationRequirements = task => task.State < TaskState.Finished,
                        SelfAction = async (task, self) =>
                        {
                            var change = await task.ChangeStateAsync(TaskState.Canceled, sessionId: SessionId);
                            await self.TemporarySetTextIfErr(change);

                            if (change) await LoadSetItems(data);
                        },
                    });
                }
            }
            class LocalTaskManager : NormalTaskManager
            {
                protected override Task<IReadOnlyCollection<ReceivedTask>> Load() =>
                    new IReadOnlyList<ReceivedTask>[] { NodeGlobalState.Instance.QueuedTasks, NodeGlobalState.Instance.PlacedTasks, NodeGlobalState.Instance.ExecutingTasks, }
                        .SelectMany(x => x)
                        .DistinctBy(x => x.Id)
                        .ToArray()
                        .AsTask<IReadOnlyCollection<ReceivedTask>>();
            }
            class RemoteTaskManager : NormalTaskManager
            {
                protected override Control WrapGrid(DataGrid grid)
                {
                    var sidtextbox = new TextBox() { Watermark = "session id" };
                    var setsidbtn = new MPButton()
                    {
                        Text = "Set sessionid",
                        OnClickSelf = async self =>
                        {
                            SessionId = string.IsNullOrWhiteSpace(sidtextbox.Text) ? Settings.SessionId : sidtextbox.Text.Trim();
                            grid.Items = await Load();
                        },
                    };
                    var sidgrid = new Grid()
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("* Auto"),
                        Children =
                        {
                            sidtextbox.WithColumn(0),
                            setsidbtn.WithColumn(1),
                        },
                    };

                    return new Grid()
                    {
                        RowDefinitions = RowDefinitions.Parse("Auto *"),
                        Children =
                        {
                            sidgrid.WithRow(0),
                            base.WrapGrid(grid).WithRow(1),
                        },
                    };
                }


                protected override async Task<IReadOnlyCollection<ReceivedTask>> Load() =>
                    (await Apis.GetMyTasksAsync(Enum.GetValues<TaskState>(), sessionId: SessionId)).ThrowIfError()
                        .Append(new DbTaskFullState("asd", "asd", TaskPolicy.AllNodes, new("asd", 1423), new MPlusTaskInputInfo("asd"), new MPlusTaskOutputInfo("be.jpg", "dir"), new() { ["type"] = "EditVideo" }) { State = TaskState.Input })
                        .ToArray();
            }
            class WatchingTaskManager : TaskManager<WatchingTaskInfo>
            {
                protected override void CreateColumns(DataGrid data)
                {
                    data.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding(nameof(WatchingTaskInfo.Id)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Policy", Binding = new Binding(nameof(WatchingTaskInfo.Policy)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Action", Binding = new Binding(nameof(WatchingTaskInfo.TaskAction)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Input", Binding = new Binding($"{nameof(WatchingTaskInfo.Source)}.Type") });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Output", Binding = new Binding($"{nameof(WatchingTaskInfo.Output)}.Type") });

                    data.Columns.Add(new DataGridTextColumn() { Header = "Paused", Binding = new Binding(nameof(WatchingTaskInfo.IsPaused)) });

                    data.Columns.Add(new DataGridButtonColumn<WatchingTaskInfo>()
                    {
                        Header = "Delete",
                        Text = "Delete",
                        SelfAction = async (task, self) =>
                        {
                            var result = await LocalApi.Send($"tasks/delwatching?taskid={task.Id}");
                            await self.TemporarySetTextIfErr(result);

                            if (result) await LoadSetItems(data);
                        },
                    });
                    data.Columns.Add(new DataGridButtonColumn<WatchingTaskInfo>()
                    {
                        Header = "Pause/Unpause",
                        Text = "Pause/Unpause",
                        SelfAction = async (task, self) =>
                        {
                            var result = await LocalApi.Send<WatchingTaskInfo>($"tasks/pausewatching?taskid={task.Id}");
                            await self.TemporarySetTextIfErr(result);

                            if (result) await LoadSetItems(data);
                        },
                    });
                }

                protected override Task<IReadOnlyCollection<WatchingTaskInfo>> Load() =>
                    (NodeGlobalState.Instance.WatchingTasks as IReadOnlyCollection<WatchingTaskInfo>).AsTask();
            }


            class ObjectToJsonConverter : IValueConverter
            {
                public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => JsonConvert.SerializeObject(value);
                public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => JsonConvert.DeserializeObject((string) value!, targetType);
            }
            class DataGridButtonColumn<T> : DataGridColumn
            {
                public string? Text;
                public Action<T>? Action;
                public Action<T, MPButton>? SelfAction;
                public Func<T, bool>? CreationRequirements;

                protected override IControl GenerateElement(DataGridCell cell, object dataItem)
                {
                    if (dataItem is not T item) return new Control();

                    var btn = new MPButton()
                    {
                        Text = Text ?? string.Empty,
                        OnClick = () => Action?.Invoke(item),
                        OnClickSelf = self => SelfAction?.Invoke(item, self),
                    };
                    btn.Bind(MPButton.IsVisibleProperty, new Binding("") { Converter = new FuncValueConverter<T, bool>(t => t is null ? false : CreationRequirements?.Invoke(t) ?? true) });

                    return btn;
                }

                protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding) => throw new NotImplementedException();
                protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs) => throw new NotImplementedException();
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
        class SettingsTab : Panel
        {
            public SettingsTab()
            {
                var nick = CreateNick();
                nick.MaxWidth = 400;
                nick.MaxHeight = 200;
                Children.Add(nick);
            }

            Grid CreateNick()
            {
                var nicktb = new TextBox()
                {

                };
                Settings.BNodeName.Bindable.SubscribeChanged(() => Dispatcher.UIThread.Post(() => nicktb.Text = Settings.NodeName), true);

                var nicksbtn = new MPButton()
                {
                    Text = new("set nick"),
                };
                nicksbtn.OnClick += async () =>
                {
                    using var _ = new FuncDispose(() => Dispatcher.UIThread.Post(() => nicksbtn.IsEnabled = true));
                    nicksbtn.IsEnabled = false;

                    var nick = nicktb.Text.Trim();
                    if (Settings.NodeName == nick)
                    {
                        Dispatcher.UIThread.Post(() => nicksbtn.Text = new($"cant change nick to the same nick nick name nick nick name\nnick"));
                        return;
                    }

                    var set = await LocalApi.Send($"setnick?nick={HttpUtility.UrlEncode(nick)}").ConfigureAwait(false);
                    Settings.Reload();

                    if (set) Dispatcher.UIThread.Post(() => nicksbtn.Text = new($"nick change good, new nick = {Settings.NodeName}"));
                    else Dispatcher.UIThread.Post(() => nicksbtn.Text = new(set.AsString()));
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
        }
        class TorrentTab : Panel
        {
            public TorrentTab() => CreateTorrentUI();

            void CreateTorrentUI()
            {
                var info = new TextBlock() { Text = "loading info..." };
                var info2 = new TextBlock() { Text = $"waiting for upload.." };

                var urltb = new TextBox() { Text = "URL" };
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
                        urltb.WithRow(2),
                        dirtb.WithRow(3),
                        button.WithRow(4),
                    },
                };
                Children.Add(torrentgrid);

                PortForwarding.GetPublicIPAsync().ContinueWith(t => Dispatcher.UIThread.Post(() =>
                    info.Text = $"this pc:  pub ip: {t.Result}  pub port: {PortForwarding.Port}  torrent port: {TorrentClient.ListenPort}"
                ));


                async void click()
                {
                    var url = urltb.Text.Trim();
                    var dir = dirtb.Text.Trim();

                    if (!Directory.Exists(dir))
                    {
                        button.Text = new("err dir not found");
                        return;
                    }

                    try
                    {
                        var client = new HttpClient();
                        var get = await LocalApi.Send<string>($"uploadtorrent?url={HttpUtility.UrlEncode(url)}&dir={HttpUtility.UrlEncode(dir)}").ConfigureAwait(false);
                        if (!get)
                        {
                            Dispatcher.UIThread.Post(() => button.Text = new("err " + get.AsString()));
                            return;
                        }
                        var hash = InfoHash.FromHex(get.Value);

                        new Thread(async () =>
                        {
                            bool die = false;
                            while (true)
                            {
                                var info = await LocalApi.Send<JObject>(url, $"torrentinfo?hash={HttpUtility.UrlEncode(hash.ToHex())}").ConfigureAwait(false);
                                if (!info)
                                {
                                    Thread.Sleep(200);
                                    continue;
                                }
                                var ifo = info.Value;

                                try
                                {
                                    if (ifo["progress"]?.Value<double>() > 99)
                                        Dispatcher.UIThread.Post(() => info2.Text = ifo.ToString(Newtonsoft.Json.Formatting.None) + "\nUpload completed.");

                                    if (!die && ifo["progress"]?.Value<double>() > 99)
                                    {
                                        die = true;
                                        Thread.Sleep(500);
                                        continue;
                                    }
                                }
                                catch { }

                                if (die)
                                {
                                    await client.GetAsync($"http://127.0.0.1:{Settings.LocalListenPort}/stoptorrent?url={hash.ToHex()}").ConfigureAwait(false);
                                    break;
                                }

                                Dispatcher.UIThread.Post(() => info2.Text = ifo.ToString(Newtonsoft.Json.Formatting.None));
                                Thread.Sleep(200);
                            }
                        })
                        { IsBackground = true }.Start();
                    }
                    catch { Dispatcher.UIThread.Post(() => button.Text = new("LOCAL connection error")); }
                }
            }
        }
        class RegistryTab : Panel
        {
            public RegistryTab() => Reload().Consume();

            async Task Reload()
            {
                Children.Clear();

                var softlist = (await Apis.GetSoftwareAsync()).ThrowIfError();
                Children.Add(new StackPanel()
                {
                    Children =
                    {
                        new MPButton()
                        {
                            Text = "+ add soft",
                            Margin = new Thickness(0, 0, 0, bottom: 20),
                            OnClickSelf = addSoft,
                        },
                        NamedList.Create("Software Registry", softlist, x => softToControl(x.Key, x.Value)),
                    },
                });


                static Task setTextTimed(MPButton button, string text, int duration) => button.TemporarySetText(text, duration);
                async void addSoft(MPButton button)
                {
                    var softname = "NewSoftTodo";
                    var soft = new SoftwareDefinition("New Soft Todo", ImmutableDictionary<string, SoftwareVersionDefinition>.Empty, null, ImmutableArray<string>.Empty);

                    var send = await LocalApi.Post(Settings.RegistryUrl, $"addsoft?name={HttpUtility.UrlEncode(softname)}",
                        new StringContent(JsonConvert.SerializeObject(soft)) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                    if (!send) await setTextTimed(button, "err " + send.AsString(), 2000);
                    else await Reload();
                }
                IControl softToControl(string softname, SoftwareDefinition soft)
                {
                    // return TaskCreationWindow.Settings.Create(new("_aed_", JObject.FromObject(soft)), FieldDescriber.Create(typeof(SoftwareDefinition)));

                    var softnametb = new TextBox() { Text = soft.VisualName };
                    var addnewbtn = new MPButton()
                    {
                        Text = "+ add version",
                        OnClick = async () =>
                        {
                            var vername = "1.0.0-todo";
                            var ver = new SoftwareVersionDefinition("<installscript>");

                            var send = await LocalApi.Post(Settings.RegistryUrl, $"addver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}",
                                new StringContent(JsonConvert.SerializeObject(ver)) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                            await Reload();
                        },
                    };

                    var content = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Children =
                        {
                            new MPButton() { Text = "update software", OnClickSelf = updateSoft, },
                            softnametb,
                            addnewbtn,
                            NamedList.Create(softname, soft.Versions,
                                x => new Expander()
                                {
                                    Header = x.Key,
                                    Margin = new Thickness(left: 20, 0, 0, 0),
                                    Content = verToControl(x.Key, x.Value),
                                }
                            ),
                        },
                    };

                    return new Expander()
                    {
                        Header = softname,
                        Content = content,
                    };


                    async void updateSoft(MPButton button)
                    {
                        var json = new JObject()
                        {
                            ["VisualName"] = softnametb.Text,
                        };

                        var send = await LocalApi.Post(Settings.RegistryUrl, $"editsoft?name={HttpUtility.UrlEncode(softname)}",
                            new StringContent(json.ToString()) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                        await setTextTimed(button, send ? "send!!!!" : ("err " + send.AsString()), 2000);
                    }
                    IControl verToControl(string vername, SoftwareVersionDefinition version)
                    {
                        var delbtn = new MPButton()
                        {
                            Text = "!!! DELETE VERSION !!!",
                            Margin = new Thickness(0, 0, 0, bottom: 20),
                            OnClickSelf = deleteVersion,
                        };

                        var versiontb = new TextBox() { Text = vername };
                        var installtb = new TextBox()
                        {
                            AcceptsReturn = true,
                            AcceptsTab = true,
                            Text = version.InstallScript,
                        };

                        var updatebtn = new MPButton() { Text = "send", OnClickSelf = updateVersion, };

                        return new StackPanel()
                        {
                            Margin = new Thickness(20, 0, 0, 0),
                            Orientation = Orientation.Vertical,
                            Children =
                            {
                                delbtn,
                                versiontb,
                                installtb,
                                updatebtn,
                            },
                        };


                        async void updateVersion(MPButton updatebtn)
                        {
                            var json = new JObject()
                            {
                                ["InstallScript"] = installtb.Text,
                            };

                            var send = await LocalApi.Post(Settings.RegistryUrl, $"editver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}&newversion={HttpUtility.UrlEncode(versiontb.Text)}",
                                new StringContent(json.ToString()) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } });

                            await setTextTimed(updatebtn, send ? "send!!!!" : ("err " + send.AsString()), 2000);
                        }
                        async void deleteVersion(MPButton delbtn)
                        {
                            var send = await LocalApi.Send(Settings.RegistryUrl, $"delver?name={HttpUtility.UrlEncode(softname)}&version={HttpUtility.UrlEncode(vername)}");
                            if (!send) await setTextTimed(delbtn, "err " + send.AsString(), 2000);
                            else await Reload();
                        }
                    }
                }
            }
        }
        class LogsTab : Panel
        {
            public LogsTab()
            {
                var tab = new TabbedControl();
                Children.Add(tab);

                tab.Add("node", new LogViewer("Node"));
                tab.Add("nodeui", new LogViewer("NodeUI"));
                tab.Add("ONLY WORKS WHEN TEXTBOX IS FOCUSED", new Panel());
            }


            class LogViewer : Panel
            {
                public LogViewer(string logName)
                {
                    var flogname = logName;
                    string getlogdir() => Path.Combine(Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!, "logs", logName, "Debug", "log.log").Replace("NodeUI", flogname);

                    var dir = getlogdir();
                    if (!File.Exists(dir))
                    {
                        logName = "dotnet";
                        dir = getlogdir();
                    }

                    var tb = new TextBox() { AcceptsReturn = true };
                    Children.Add(new Grid()
                    {
                        RowDefinitions = RowDefinitions.Parse("Auto *"),
                        Children =
                        {
                            new TextBlock() { Text = dir }.WithRow(0),
                            tb.WithRow(1),
                        },
                    });


                    new Thread(() =>
                    {
                        var buffer = new byte[1024 * 8];

                        while (true)
                        {
                            Thread.Sleep(1000);

                            try
                            {
                                var visible = Dispatcher.UIThread.InvokeAsync(() => tb.IsFocused).Result;
                                if (!visible) continue;

                                int read = 0;
                                if (File.Exists(dir))
                                {
                                    using var reader = File.Open(dir, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                    reader.Position = Math.Max(reader.Length - buffer.Length, 0);
                                    read = reader.Read(buffer);
                                }

                                var str = $"{Encoding.UTF8.GetString(buffer.AsSpan(0, read))}\n\n<read on {DateTime.Now}>";
                                Dispatcher.UIThread.Post(() =>
                                {
                                    tb.Text = str;
                                    tb.CaretIndex = tb.Text.Length;
                                });
                            }
                            catch { }
                        }
                    })
                    { IsBackground = true }.Start();
                }
            }
        }
    }
}