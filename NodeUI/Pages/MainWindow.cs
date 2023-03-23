using System.Globalization;
using System.Text;
using System.Web;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodeToUI.Requests;
using NodeUI.Pages.MainWindowTabs;

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
            tabs.Add("menu.settings", new SettingsTab());
            tabs.Add("logs", new LogsTab());
            if (Init.DebugFeatures) tabs.Add("registry", new RegistryTab());
            tabs.Add("cgtraderupload", new CGTraderUploadTab());
            tabs.Add("3dupload", new ModelUploader());

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


            NodeStateUpdater.IsConnectedToNode.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
            {
                if (NodeStateUpdater.IsConnectedToNode.Value) statustb.Text = null;
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
                    if (request is CaptchaRequest captchareq) handleCaptchaRequest(captchareq);
                    else if (request is InputRequest inputreq) handleInputRequest(inputreq);


                    void handleCaptchaRequest(CaptchaRequest req)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            var window = new CaptchaWindow(req.Base64Image, v => sendResponse(v));
                            req.OnRemoved = () => Dispatcher.UIThread.Post(() => { try { window.ForceClose(); } catch { } });
                            window.Show();
                        });
                    }
                    void handleInputRequest(InputRequest req)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            var window = new InputWindow(req.Text, v => sendResponse(v));
                            req.OnRemoved = () => Dispatcher.UIThread.Post(() => { try { window.ForceClose(); } catch { } });
                            window.Show();
                        });
                    }


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
                            NamedList.Create("Software stats", NodeGlobalState.Instance.SoftwareStats, softToControl),
                            NamedList.Create("Our plugins", NodeGlobalState.Instance.InstalledPlugins, pluginToControl),
                        },
                    },
                };

                Children.Add(scroll);


                IControl softToControl(KeyValuePair<string, SoftwareStats> value)
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
                    pluginslist.SelectionChanged += (obj, e) => versionslist.Items = NodeGlobalState.Instance.SoftwareStats.Value.GetValueOrDefault(pluginslist.SelectedItem)?.ByVersion.Keys.ToArray() ?? Array.Empty<string>();
                    pluginslist.SelectedIndex = 0;

                    var cp = NodeGlobalState.Instance.SoftwareStats.GetBoundCopy();
                    cp.SubscribeChanged(() => Dispatcher.UIThread.Post(() =>
                    {
                        pluginslist.Items = NodeGlobalState.Instance.SoftwareStats.Value.Keys.ToArray();
                        pluginslist.SelectedIndex = pluginslist.SelectedIndex;
                    }), true);
                    Bindable = cp;

                    var installbtn = new MPButton()
                    {
                        Text = "Install plugin",
                        OnClickSelf = async self =>
                        {
                            var res = await LocalApi.Default.Get("deploy", "Installing plugin", ("type", pluginslist.SelectedItem), ("version", versionslist.SelectedItem));
                            await self.FlashErrorIfErr(res);
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

                                var tasks = await Apis.Default.GetMyTasksAsync(new[] { TaskState.Queued, TaskState.Input, TaskState.Active, TaskState.Output, TaskState.Validation, });
                                await self.FlashErrorIfErr(tasks);
                                if (!tasks) return;

                                if (allplaced.Children.Count > 1)
                                    allplaced.Children.RemoveAt(1);
                                allplaced.Children.Add(NamedList.CreateRaw("ALL active placed tasks", tasks.ThrowIfError(), placedTasksCreate));
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
                            var cstate = await Apis.Default.ChangeStateAsync(task, TaskState.Canceled);
                            await self.FlashErrorIfErr(cstate);
                            if (!cstate) return;

                            await updateState(self);
                        },
                    };

                    async Task updateState(MPButton button)
                    {
                        var state = await Apis.Default.GetTaskStateAsyncOrThrow(task);
                        await button.FlashErrorIfErr(state);
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
                IControl watchingTasksCreate(WatchingTask task)
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
                                new TextBlock() { Text = $"Source: {JsonConvert.SerializeObject(task.Source, Formatting.None)}" },
                                new TextBlock() { Text = $"Output: {JsonConvert.SerializeObject(task.Output, Formatting.None)}" },
                            },
                        },
                    };
                }
            }
        }

        class TasksTab2 : Panel
        {
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
                protected NodeCommon.Apis Api => Apis.Default;

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
            abstract class NormalTaskManager : TaskManager<TaskBase>
            {
                protected override void CreateColumns(DataGrid data)
                {
                    data.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding(nameof(ReceivedTask.Id)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "State", Binding = new Binding(nameof(ReceivedTask.State)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Action", Binding = new Binding(nameof(ReceivedTask.Action)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Input", Binding = new Binding("Input.Type") });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Output", Binding = new Binding("Output.Type") });

                    data.Columns.Add(new DataGridTextColumn() { Header = "Server Host", Binding = new Binding($"{nameof(ServerTaskFullState.Server)}.{nameof(TaskServer.Host)}") });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Server Userid", Binding = new Binding($"{nameof(ServerTaskFullState.Server)}.{nameof(TaskServer.Userid)}") });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Server Nickname", Binding = new Binding($"{nameof(ServerTaskFullState.Server)}.{nameof(TaskServer.Nickname)}") });

                    data.Columns.Add(new DataGridButtonColumn<DbTaskFullState>()
                    {
                        Header = "Cancel task",
                        Text = "Cancel task",
                        CreationRequirements = task => task.State < TaskState.Finished,
                        SelfAction = async (task, self) =>
                        {
                            var change = await Api.ChangeStateAsync(task, TaskState.Canceled);
                            await self.FlashErrorIfErr(change);

                            if (change) await LoadSetItems(data);
                        },
                    });
                }
            }
            class LocalTaskManager : NormalTaskManager
            {
                protected override Task<IReadOnlyCollection<TaskBase>> Load() =>
                    new IReadOnlyList<TaskBase>[] { NodeGlobalState.Instance.QueuedTasks, NodeGlobalState.Instance.PlacedTasks, NodeGlobalState.Instance.ExecutingTasks, }
                        .SelectMany(x => x)
                        .DistinctBy(x => x.Id)
                        .ToArray()
                        .AsTask<IReadOnlyCollection<TaskBase>>();
            }
            class RemoteTaskManager : NormalTaskManager
            {
                protected override async Task<IReadOnlyCollection<TaskBase>> Load() =>
                    await Api.GetMyTasksAsync(Enum.GetValues<TaskState>()).ThrowIfError();
            }
            class WatchingTaskManager : TaskManager<WatchingTask>
            {
                protected override void CreateColumns(DataGrid data)
                {
                    data.Columns.Add(new DataGridTextColumn() { Header = "ID", Binding = new Binding(nameof(WatchingTask.Id)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Policy", Binding = new Binding(nameof(WatchingTask.Policy)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Action", Binding = new Binding(nameof(WatchingTask.TaskAction)) });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Input", Binding = new Binding($"{nameof(WatchingTask.Source)}.Type") });
                    data.Columns.Add(new DataGridTextColumn() { Header = "Output", Binding = new Binding($"{nameof(WatchingTask.Output)}.Type") });

                    data.Columns.Add(new DataGridTextColumn() { Header = "Paused", Binding = new Binding(nameof(WatchingTask.IsPaused)) });

                    data.Columns.Add(new DataGridButtonColumn<WatchingTask>()
                    {
                        Header = "Delete",
                        Text = "Delete",
                        SelfAction = async (task, self) =>
                        {
                            var result = await LocalApi.Default.Get("tasks/delwatching", "Deleting watching task", ("taskid", task.Id));
                            await self.FlashErrorIfErr(result);

                            if (result) await LoadSetItems(data);
                        },
                    });
                    data.Columns.Add(new DataGridButtonColumn<WatchingTask>()
                    {
                        Header = "Pause/Unpause",
                        Text = "Pause/Unpause",
                        SelfAction = async (task, self) =>
                        {
                            var result = await LocalApi.Default.Get<WatchingTask>("tasks/pausewatching", "Pausing watching task", ("taskid", task.Id));
                            await self.FlashErrorIfErr(result);

                            if (result) await LoadSetItems(data);
                        },
                    });
                }

                protected override Task<IReadOnlyCollection<WatchingTask>> Load() =>
                    (NodeGlobalState.Instance.WatchingTasks as IReadOnlyCollection<WatchingTask>).AsTask();
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
        class LogsTab : Panel
        {
            public LogsTab()
            {
                var tab = new TabbedControl();
                Children.Add(tab);

                tab.Add("node", new LogViewer("Node", LogLevel.Debug));
                tab.Add("nodeui", new LogViewer("NodeUI", LogLevel.Debug));
                tab.Add("node-trace", new LogViewer("Node", LogLevel.Trace));
                tab.Add("nodeui-trace", new LogViewer("NodeUI", LogLevel.Trace));
                tab.Add("ONLY WORKS WHEN TEXTBOX IS FOCUSED", new Panel());
            }


            class LogViewer : Panel
            {
                public LogViewer(string logName, LogLevel level)
                {
                    var flogname = logName;
                    string getlogdir() => Path.Combine(Path.GetDirectoryName(typeof(MainWindow).Assembly.Location)!, "logs", logName, level.Name, "log.log").Replace("NodeUI", flogname);

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

                                var str = $"{Encoding.UTF8.GetString(buffer.AsSpan(0, read))}\n\n<read on {DateTime.UtcNow}>";
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