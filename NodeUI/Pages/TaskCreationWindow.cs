using Avalonia.Controls.Templates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaskCreationInfo = NodeCommon.Tasks.UITaskCreationInfo;

namespace NodeUI.Pages
{
    public class TaskCreationWindow : Window
    {
        public TaskCreationWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.FixStartupLocation();
            Width = 692;
            Height = 410;
            Title = App.AppName;
            Icon = App.Icon;

            Content = new TaskCreationPanel();
        }

        static Task Post(Action action) => Dispatcher.UIThread.InvokeAsync(action);
        static Task<T> Post<T>(Func<T> action) => Dispatcher.UIThread.InvokeAsync(action);

        void ShowPart(TaskPart part)
        {
            Content = new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto *"),
                Children =
                {
                    new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 20,
                    }.Bind(part.Title).WithRow(0),
                    part.WithRow(1),
                },
            };
        }

        static TypedListBox<T> CreateListBox<T>(IReadOnlyCollection<T> items, Func<T, IControl> func) => new TypedListBox<T>(items, func);


        class TypedListBox<T> : ListBox, IStyleable
        {
            Type IStyleable.StyleKey => typeof(ListBox);
            public new T SelectedItem => (T) base.SelectedItem!;

            public TypedListBox(IReadOnlyCollection<T> items, Func<T, IControl> func)
            {
                Items = items;
                ItemTemplate = new FuncDataTemplate<T>((t, _) => func(t));
            }
        }
        class TaskPartContainer : Grid
        {
            readonly TextBlock TitleTextBlock;
            readonly MPButton BackButton, NextButton;
            readonly Stack<TaskPart> Parts = new();

            public TaskPartContainer(TaskPart firstPart)
            {
                RowDefinitions = RowDefinitions.Parse("Auto Auto *");

                TitleTextBlock = new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 20,
                };
                Children.Add(TitleTextBlock.WithRow(1));

                BackButton = new MPButton()
                {
                    Text = new("<<<"),
                    OnClick = ShowPrev,
                };
                NextButton = new MPButton()
                {
                    Text = new(">>>"),
                    OnClick = ShowNext,
                };

                var buttonsgrid = new Grid()
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("* *"),
                    Children =
                    {
                        BackButton.WithColumn(0),
                        NextButton.WithColumn(1),
                    }
                };
                Children.Add(buttonsgrid.WithRow(0));

                ShowPart(firstPart);
            }

            void ShowPart(TaskPart part)
            {
                if (Parts.TryPeek(out var prev))
                    prev.IsVisible = false;

                Parts.Push(part);
                Children.Add(part.WithRow(2));
                TitleTextBlock.Bind(part.Title);

                part.OnChoose += UpdateButtons;
                UpdateButtons(false);

            }
            void ShowNext()
            {
                if (!Parts.TryPeek(out var current)) return;
                current.OnNext();

                var next = current.Next;
                if (next is null) return;

                ShowPart(next);
                Dispatcher.UIThread.Post(next.Initialize);
            }
            void ShowPrev()
            {
                if (!Parts.TryPop(out var current)) return;
                Children.Remove(current);

                if (!Parts.TryPeek(out var prev)) return;
                prev.IsVisible = true;

                TitleTextBlock.Bind(prev.Title);
                UpdateButtons(true);
            }

            void UpdateButtons(bool canMoveNext)
            {
                BackButton.IsEnabled = Parts.Count > 1;
                NextButton.IsEnabled = canMoveNext;
            }
        }

        abstract class TaskPart : Panel
        {
            public abstract event Action<bool>? OnChoose;
            public abstract LocalizedString Title { get; }
            public abstract TaskPart? Next { get; }

            protected readonly TaskCreationInfo Builder;

            protected TaskPart(TaskCreationInfo builder) => Builder = builder;

            public virtual void Initialize() { }
            public virtual void OnNext() { }
        }

        abstract class PolicyTaskPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Policy");

            protected PolicyTaskPart(TaskCreationInfo builder) : base(builder) { }

            public override void Initialize()
            {
                var list = TypedListBox.Create(Enum.GetValues<TaskPolicy>(), t => new TextBlock() { Text = t.ToString() });
                list.SelectionChanged += (obj, e) =>
                {
                    OnChoose?.Invoke(list.SelectedItems.Count != 0);
                    Builder.Policy = list.SelectedItem;
                };

                Children.Add(list);
                Dispatcher.UIThread.Post(() => list.SelectedIndex = 0);
            }
        }
        abstract class ChoosePluginPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Plugin");

            public ChoosePluginPart(TaskCreationInfo builder) : base(builder) { }

            public override void Initialize()
            {
                var plugins = Enum.GetValues<PluginType>();
                if (Builder.Policy == TaskPolicy.SameNode)
                    plugins = NodeGlobalState.Instance.InstalledPlugins.Value.Select(x => x.Type).ToArray();

                var list = CreateListBox(plugins, type => new TextBlock() { Text = type.GetName() });
                list.SelectionChanged += (obj, e) =>
                {
                    OnChoose?.Invoke(list.SelectedItems.Count != 0);
                    Builder.Type = list.SelectedItem;
                };

                Children.Add(list);
            }
        }
        abstract class ChooseVersionPart : TaskPart
        {
            protected const string AnyVersion = "<any>";

            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new($"Choose {Builder.Type.GetName()} Version");

            public ChooseVersionPart(TaskCreationInfo builder) : base(builder) { }

            public override void Initialize()
            {
                IEnumerable<string> versions;
                if (NodeGlobalState.Instance.SoftwareStats.Value.TryGetValue(Builder.Type.ToString(), out var stats))
                    versions = stats.ByVersion.Keys;
                else versions = Enumerable.Empty<string>();

                var list = CreateListBox(versions.Prepend(AnyVersion).ToArray(), version => new TextBlock() { Text = version });
                list.SelectionChanged += (obj, e) =>
                {
                    Builder.Version = list.SelectedItem == AnyVersion ? null : list.SelectedItem;
                    OnChoose?.Invoke(list.SelectedItems.Count != 0);
                };

                Children.Add(list);
                Dispatcher.UIThread.Post(() => list.SelectedIndex = 0);
            }
        }
        abstract class ChooseActionPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Action");

            public ChooseActionPart(TaskCreationInfo builder) : base(builder) { }

            public override void Initialize()
            {
                var list = CreateListBox(NodeGlobalState.Instance.TaskDefinitions.Value.Actions.Where(x => x.RequiredPlugins.Contains(Builder.Type)).ToArray(), action => new TextBlock() { Text = action.Name });
                list.SelectionChanged += (obj, e) =>
                {
                    Builder.Action = list.SelectedItem.Name;
                    OnChoose?.Invoke(list.SelectedItems.Count != 0);
                };

                Children.Add(list);
            }
        }

        abstract class ChooseInputOutputPartBase<T> : TaskPart
        {
            public sealed override event Action<bool>? OnChoose;
            protected Panel SettingPanel { get; private set; } = null!;

            public ChooseInputOutputPartBase(TaskCreationInfo builder) : base(builder) { }

            protected void Init(IReadOnlyList<T> describers, Func<T, IControl> templateFunc)
            {
                var types = new ComboBox()
                {
                    Items = describers,
                    ItemTemplate = new FuncDataTemplate<T>((t, _) => t is null ? null : templateFunc(t)),
                    SelectedIndex = 0,
                };

                SettingPanel = new Panel();
                var grid = new Grid()
                {
                    RowDefinitions = RowDefinitions.Parse("Auto *"),
                    Children =
                    {
                        types.WithRow(0),
                        SettingPanel.WithRow(1),
                    },
                };
                Children.Add(grid);

                types.Subscribe(ComboBox.SelectedItemProperty, item => OnSetItem((T) item!));
                Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
            }

            protected abstract void OnSetItem(T item);
        }
        abstract class ChooseInputOutputPart : ChooseInputOutputPartBase<TaskInputOutputDescriber>
        {
            protected JObject InputOutputJson => (JObject) Setting.Property.Value;
            JsonUISetting.Setting Setting = null!;

            public ChooseInputOutputPart(TaskCreationInfo builder) : base(builder) { }

            protected void InitFromCache(Func<TasksFullDescriber, ImmutableArray<TaskInputOutputDescriber>> func) => Init(func(NodeGlobalState.Instance.TaskDefinitions.Value));
            protected void Init(IReadOnlyList<TaskInputOutputDescriber> describers) => Init(describers, t => new TextBlock() { Text = t.Type });

            protected override void OnSetItem(TaskInputOutputDescriber? describer)
            {
                SettingPanel.Children.Clear();
                if (describer is null) return;

                var obj = new JObject() { ["type"] = describer.Type, };
                var parent = new JObject() { [describer.Type] = obj, };

                Setting = JsonUISetting.Create(parent.Property(describer.Type)!, describer.Object);
                SettingPanel.Children.Add(Setting);
            }
            public override void OnNext()
            {
                base.OnNext();
                Setting?.UpdateValue();
            }
        }
        abstract class ParametersPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Modify Parameters");

            JsonUISetting.Setting Setting = null!;

            public ParametersPart(TaskCreationInfo builder) : base(builder) { }

            public override void Initialize()
            {
                var describer = NodeGlobalState.Instance.TaskDefinitions.Value.Actions.First(x => x.Name == Builder.Action).DataDescriber;
                var data = Builder.Data = new JObject();

                data.RemoveAll();
                data["type"] = Builder.Action;

                var parent = new JObject() { [describer.Name] = data, };
                Setting = JsonUISetting.Create(parent.Property(describer.Name)!, describer);
                Children.Add(Setting);

                Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
            }

            public override void OnNext()
            {
                base.OnNext();
                Setting.UpdateValue();
            }
        }
        abstract class EndPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Waiting");
            public override TaskPart? Next => null;

            protected abstract string StartTaskEndpoint { get; }

            readonly TextBlock StatusTextBlock;

            public EndPart(TaskCreationInfo builder) : base(builder)
            {
                Children.Add(StatusTextBlock = new TextBlock());
                StartTaskAsync().Consume();
            }

            static void ForEachProperty(JToken jobj, Action<JObject, JProperty> func)
            {
                foreach (var (parent, property) in getProperties(jobj).ToArray())
                    func(parent, property);

                IEnumerable<(JObject parent, JProperty property)> getProperties(JToken token) =>
                    token is not JObject obj
                        ? Enumerable.Empty<(JObject, JProperty)>()
                        : obj.Properties().Select(p => (obj, p))
                            .Concat(obj.Values().SelectMany(getProperties));
            }
            static void ProcessObject(JToken jobj)
            {
                ForEachProperty(jobj, (parent, property) =>
                {
                    // remove null
                    if (property.Value is null || property.Value?.Type == JTokenType.Null)
                        parent.Remove(property.Name);
                });
            }

            async Task StartTaskAsync()
            {
                ProcessObject(Builder.Data);
                ProcessObject(Builder.Input);
                ProcessObject(Builder.Output);

                var serialized = JsonConvert.SerializeObject(Builder, JsonSettings.LowercaseIgnoreNull);
                var taskid = await LocalApi.Default.Post<string>(StartTaskEndpoint, "Starting a task", new StringContent(serialized)).ConfigureAwait(false);
                if (!taskid)
                {
                    Dispatcher.UIThread.Post(() => StatusTextBlock.Text = $"error {taskid}");
                    return;
                }

                Dispatcher.UIThread.Post(() => StatusTextBlock.Text = $"task {taskid} created");
            }
        }


        class TaskCreationPanel : Panel
        {
            public TaskCreationPanel() => Children.Add(new TaskPartContainer(new ChooseTypePart()));


            class ChooseTypePart : TaskCreationWindow.TaskPart
            {
                public override event Action<bool>? OnChoose;
                public override LocalizedString Title => new("Choose Type");

                public override TaskPart? Next => TypesList.SelectedItem switch
                {
                    TaskCreationType.Normal => new NormalTaskCreationPanel.LocalRemotePart(new()),
                    TaskCreationType.WatchingRepeating => new WatchingTaskCreationPanel.LocalRemotePart(new()),
                    _ => throw new InvalidOperationException(),
                };

                readonly TypedListBox<TaskCreationType> TypesList;

                public ChooseTypePart() : base(new())
                {
                    TypesList = new TypedListBox<TaskCreationType>(Enum.GetValues<TaskCreationType>(), t => new TextBlock() { Text = t.ToString() });
                    TypesList.SelectionChanged += (obj, e) => OnChoose?.Invoke(TypesList.SelectedItems.Count != 0);
                    Children.Add(TypesList);

                    Dispatcher.UIThread.Post(() => TypesList.SelectedIndex = 0);
                }


                enum TaskCreationType { Normal, WatchingRepeating, }
            }
        }

        static class NormalTaskCreationPanel
        {
            public class LocalRemotePart : TaskCreationWindow.PolicyTaskPart
            {
                public override TaskPart? Next => new ChoosePluginPart(Builder);
                public LocalRemotePart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChoosePluginPart : TaskCreationWindow.ChoosePluginPart
            {
                public override TaskPart? Next => new ChooseVersionPart(Builder);
                public ChoosePluginPart(TaskCreationInfo info) : base(info) { }
            }

            class ChooseVersionPart : TaskCreationWindow.ChooseVersionPart
            {
                public override TaskPart? Next => new ChooseActionPart(Builder);
                public ChooseVersionPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseActionPart : TaskCreationWindow.ChooseActionPart
            {
                public override TaskPart? Next => new ChooseInputPart(Builder);
                public ChooseActionPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseInputPart : ChooseInputOutputPart
            {
                public override LocalizedString Title => new("Choose Input");
                public override TaskPart? Next => new ChooseOutputPart(Builder);

                public ChooseInputPart(TaskCreationInfo builder) : base(builder) { }

                public override void Initialize() => InitFromCache(info => info.Inputs);
                public override void OnNext()
                {
                    base.OnNext();
                    Builder.Input = InputOutputJson;
                }
            }
            class ChooseOutputPart : TaskCreationWindow.ChooseInputOutputPart
            {
                public override LocalizedString Title => new("Choose Output");
                public override TaskPart? Next => new ChooseParametersPart(Builder);

                public ChooseOutputPart(TaskCreationInfo builder) : base(builder) { }

                public override void Initialize() => InitFromCache(info => info.Outputs);
                public override void OnNext()
                {
                    base.OnNext();
                    Builder.Output = InputOutputJson;
                }
            }
            class ChooseParametersPart : TaskCreationWindow.ParametersPart
            {
                public override TaskPart? Next => new EndPart(Builder);
                public ChooseParametersPart(TaskCreationInfo builder) : base(builder) { }
            }
            class EndPart : TaskCreationWindow.EndPart
            {
                protected override string StartTaskEndpoint => "tasks/start";

                public EndPart(TaskCreationInfo builder) : base(builder) { }
            }
        }
        static class WatchingTaskCreationPanel
        {
            public class LocalRemotePart : TaskCreationWindow.PolicyTaskPart
            {
                public override TaskPart? Next => new ChoosePluginPart(Builder);
                public LocalRemotePart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChoosePluginPart : TaskCreationWindow.ChoosePluginPart
            {
                public override TaskPart? Next => new ChooseVersionPart(Builder);

                public ChoosePluginPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseVersionPart : TaskCreationWindow.ChooseVersionPart
            {
                public override TaskPart? Next => new ChooseActionPart(Builder);
                public ChooseVersionPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseActionPart : TaskCreationWindow.ChooseActionPart
            {
                public override TaskPart? Next => new ChooseInputPart(Builder);
                public ChooseActionPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseInputPart : ChooseInputOutputPart
            {
                public override LocalizedString Title => new("Choose Input");
                public override TaskPart? Next => new ChooseOutputPart(Builder);

                public ChooseInputPart(TaskCreationInfo builder) : base(builder) { }

                public override void Initialize() => InitFromCache(info => info.WatchingInputs);
                public override void OnNext()
                {
                    base.OnNext();
                    Builder.Input = InputOutputJson;
                }
            }
            class ChooseOutputPart : TaskCreationWindow.ChooseInputOutputPart
            {
                public override LocalizedString Title => new("Choose Output");
                public override TaskPart? Next => new ChooseParametersPart(Builder);

                public ChooseOutputPart(TaskCreationInfo builder) : base(builder) { }

                public override void Initialize() => InitFromCache(info => info.WatchingOutputs);
                public override void OnNext()
                {
                    base.OnNext();
                    Builder.Output = InputOutputJson;
                }
            }
            class ChooseParametersPart : TaskCreationWindow.ParametersPart
            {
                public override TaskPart? Next => new EndPart(Builder);
                public ChooseParametersPart(TaskCreationInfo builder) : base(builder) { }
            }

            class EndPart : TaskCreationWindow.EndPart
            {
                protected override string StartTaskEndpoint => "tasks/startwatching";

                public EndPart(TaskCreationInfo builder) : base(builder) { }
            }
        }
    }
}