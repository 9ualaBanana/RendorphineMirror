using System.Text.RegularExpressions;
using Avalonia.Controls.Templates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                RowDefinitions = RowDefinitions.Parse("Auto * Auto");

                TitleTextBlock = new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 20,
                };
                Children.Add(TitleTextBlock.WithRow(0));

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
                Children.Add(buttonsgrid.WithRow(2));

                ShowPart(firstPart);
            }

            void ShowPart(TaskPart part)
            {
                if (Parts.TryPeek(out var prev))
                    prev.IsVisible = false;

                Parts.Push(part);
                Children.Add(part.WithRow(1));
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

            public virtual void OnNext() { }
        }
        abstract class ChoosePluginPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Plugin");

            protected readonly TypedListBox<PluginType> PluginsList;

            public ChoosePluginPart() : base(new())
            {
                PluginsList = CreateListBox(Enum.GetValues<PluginType>(), type => new TextBlock() { Text = type.GetName() });
                PluginsList.SelectionChanged += (obj, e) =>
                    OnChoose?.Invoke(PluginsList.SelectedItems.Count != 0);

                Children.Add(PluginsList);
            }
        }
        abstract class ChooseVersionPart : TaskPart
        {
            protected const string AnyVersion = "<any>";

            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new($"Choose {Builder.Type.GetName()} Version");

            protected readonly TypedListBox<string> VersionsList;

            public ChooseVersionPart(TaskCreationInfo builder) : base(builder)
            {
                string[] versions;
                if (UICache.SoftwareStats.Value.TryGetValue(builder.Type, out var stats))
                    versions = stats.ByVersion.Keys.ToArray();
                else versions = Array.Empty<string>();

                VersionsList = CreateListBox(versions.Prepend(AnyVersion).ToArray(), version => new TextBlock() { Text = version });
                VersionsList.SelectionChanged += (obj, e) => OnChoose?.Invoke(VersionsList.SelectedItems.Count != 0);
                Dispatcher.UIThread.Post(() => VersionsList.SelectedIndex = 0);

                Children.Add(VersionsList);
            }
        }
        abstract class ChooseActionPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Action");

            protected readonly TypedListBox<TaskActionDescriber> ActionsList;

            public ChooseActionPart(TaskCreationInfo builder) : base(builder)
            {
                var ac = UICache.GetTasksInfoAsync().GetAwaiter().GetResult();
                ActionsList = CreateListBox(UICache.GetTasksInfoAsync().GetAwaiter().GetResult().Actions, action => new TextBlock() { Text = action.Name });
                ActionsList.SelectionChanged += (obj, e) =>
                    OnChoose?.Invoke(ActionsList.SelectedItems.Count != 0);

                Children.Add(ActionsList);
            }
        }

        abstract class ChooseInputOutputPartBase<T> : TaskPart
        {
            protected readonly Panel SettingPanel;

            public ChooseInputOutputPartBase(IReadOnlyList<T> describers, Func<T, IControl> templateFunc, TaskCreationInfo builder) : base(builder)
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
            }

            protected abstract void OnSetItem(T item);
        }
        abstract class ChooseInputOutputPart : ChooseInputOutputPartBase<TaskInputOutputDescriber>
        {
            protected JObject InputOutputJson => (JObject) Setting!.Property.Value;
            Settings.ISetting? Setting;

            public ChooseInputOutputPart(ImmutableArray<TaskInputOutputDescriber> describers, TaskCreationInfo builder) : base(describers, t => new TextBlock() { Text = t.Type }, builder) { }

            protected override void OnSetItem(TaskInputOutputDescriber? describer)
            {
                SettingPanel.Children.Clear();
                if (describer is null) return;

                var obj = new JObject()
                {
                    ["$type"] = describer.Object.JsonTypeName,
                    ["type"] = describer.Type,
                };
                var parent = new JObject() { [describer.Type] = obj, };

                Setting = Settings.Create(parent.Property(describer.Type)!, describer.Object);
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

            protected readonly JObject Data = new();
            readonly Settings.ISetting Setting;

            public ParametersPart(TaskCreationInfo builder) : base(builder)
            {
                var describer = UICache.GetTasksInfoAsync().GetAwaiter().GetResult().Actions.First(x => x.Name == builder.Action).DataDescriber;
                Data.RemoveAll();
                Data["type"] = builder.Action;

                var parent = new JObject() { [describer.Name] = Data, };
                Setting = Settings.Create(parent.Property(describer.Name)!, describer);
                Children.Add(Setting);

                Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
            }

            public override void OnNext()
            {
                base.OnNext();
                Setting.UpdateValue();
            }
        }
        abstract class LocalRemoteTaskPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Type (execute locally or send to another node)");

            protected readonly TypedListBox<TType> Types;

            protected LocalRemoteTaskPart(TaskCreationInfo builder) : base(builder)
            {
                Types = new(Enum.GetValues<TType>(), t => new TextBlock() { Text = t.ToString() });
                Types.SelectionChanged += (obj, e) => OnChoose?.Invoke(Types.SelectedItems.Count != 0);
                Children.Add(Types);

                Dispatcher.UIThread.Post(() => Types.SelectedIndex = 0);
            }


            public enum TType { Remote, Local }
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
                    TaskCreationType.Normal => new NormalTaskCreationPanel.ChoosePluginPart(),
                    TaskCreationType.WatchingRepeating => new WatchingTaskCreationPanel.ChoosePluginPart(),
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
            public class ChoosePluginPart : TaskCreationWindow.ChoosePluginPart
            {
                public override TaskPart? Next => new ChooseVersionPart(Builder.With(x => x.Type = PluginsList.SelectedItem));
            }
            class ChooseVersionPart : TaskCreationWindow.ChooseVersionPart
            {
                public override TaskPart? Next => new ChooseActionPart(Builder.With(x => x.Version = (VersionsList.SelectedItem == AnyVersion ? null : VersionsList.SelectedItem)));
                public ChooseVersionPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseActionPart : TaskCreationWindow.ChooseActionPart
            {
                public override TaskPart? Next => new ChooseInputPart(Builder.With(x => x.Action = ActionsList.SelectedItem.Name));
                public ChooseActionPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseInputPart : ChooseInputOutputPart
            {
                public override event Action<bool>? OnChoose;
                public override LocalizedString Title => new("Choose Input");
                public override TaskPart? Next => new ChooseOutputPart(Builder.With(x => x.Input = InputOutputJson));

                public ChooseInputPart(TaskCreationInfo builder) : base(UICache.GetTasksInfoAsync().GetAwaiter().GetResult().Inputs, builder) =>
                    Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
            }
            class ChooseOutputPart : TaskCreationWindow.ChooseInputOutputPart
            {
                public override event Action<bool>? OnChoose;
                public override LocalizedString Title => new("Choose Output");
                public override TaskPart? Next => new ChooseParametersPart(Builder.With(x => x.Output = InputOutputJson));

                public ChooseOutputPart(TaskCreationInfo builder) : base(UICache.GetTasksInfoAsync().GetAwaiter().GetResult().Outputs, builder) =>
                    Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
            }
            class ChooseParametersPart : TaskCreationWindow.ParametersPart
            {
                public override TaskPart? Next => new LocalRemotePart(Builder.With(x => x.Data = Data));
                public ChooseParametersPart(TaskCreationInfo builder) : base(builder) { }
            }
            class LocalRemotePart : TaskCreationWindow.LocalRemoteTaskPart
            {
                public override TaskPart? Next => new WaitingPart(Builder.With(x => x.ExecuteLocally = Types.SelectedItem == TType.Local));
                public LocalRemotePart(TaskCreationInfo builder) : base(builder) { }
            }
            class WaitingPart : TaskPart
            {
                public override event Action<bool>? OnChoose;
                public override LocalizedString Title => new("Waiting");
                public override TaskPart? Next => null;

                readonly TextBlock StatusTextBlock;

                public WaitingPart(TaskCreationInfo builder) : base(builder)
                {
                    Children.Add(StatusTextBlock = new TextBlock());
                    StartTaskAsync().Consume();
                }

                string Status() => @$"
                    waiting {Builder.Action}
                    using {Builder.Type}
                    v {Builder.Version ?? "<any>"}
                    from {Builder.Input.ToString(Formatting.None)}
                    to {Builder.Output.ToString(Formatting.None)}
                    and {Builder.Data.ToString(Formatting.None)}
                    ".TrimLines();
                void Status(string? text) => Dispatcher.UIThread.Post(() => StatusTextBlock.Text = text + Environment.NewLine + Status());

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
                    var post = await LocalApi.Post<string>(LocalApi.LocalIP, "starttask", new StringContent(serialized)).ConfigureAwait(false);
                    if (!post)
                    {
                        Status($"error {post}");
                        return;
                    }

                    var taskid = post.Value;

                    var stt = "";
                    Status(stt = $"taskid={taskid}\n{stt}");

                    var token = new CancellationTokenSource();
                    Dispatcher.UIThread.Post(() => ((Window) VisualRoot!).Closed += (_, _) => token.Cancel());

                    while (true)
                    {
                        if (token.Token.IsCancellationRequested) return;

                        var stater = await Apis.GetTaskStateAsync(taskid);
                        if (!stater)
                        {
                            Status($"error getting task state {stater}\n{stt}");
                            continue;
                        }

                        var state = stater.Value;
                        Status($"task state: {state.State} {JsonConvert.SerializeObject(state)}\n{stt}".TrimLines());

                        await Task.Delay(1000);
                    }
                }
            }
        }
        static class WatchingTaskCreationPanel
        {
            public class ChoosePluginPart : TaskCreationWindow.ChoosePluginPart
            {
                public override TaskPart? Next => new ChooseVersionPart(Builder.With(x => x.Type = PluginsList.SelectedItem));
            }
            class ChooseVersionPart : TaskCreationWindow.ChooseVersionPart
            {
                public override TaskPart? Next => new ChooseActionPart(Builder.With(x => x.Version = (VersionsList.SelectedItem == AnyVersion ? null : VersionsList.SelectedItem)));
                public ChooseVersionPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseActionPart : TaskCreationWindow.ChooseActionPart
            {
                public override TaskPart? Next => new ChooseInputPart(Builder.With(x => x.Action = ActionsList.SelectedItem.Name));
                public ChooseActionPart(TaskCreationInfo builder) : base(builder) { }
            }
            class ChooseInputPart : ChooseInputOutputPart
            {
                public override event Action<bool>? OnChoose;
                public override LocalizedString Title => new("Choose Input");
                public override TaskPart? Next => new ChooseOutputPart(Builder.With(x => x.Input = InputOutputJson));

                public ChooseInputPart(TaskCreationInfo builder) : base(UICache.GetTasksInfoAsync().GetAwaiter().GetResult().WatchingInputs, builder) =>
                    Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
            }
            class ChooseOutputPart : TaskCreationWindow.ChooseInputOutputPart
            {
                public override event Action<bool>? OnChoose;
                public override LocalizedString Title => new("Choose Output");
                public override TaskPart? Next => new ChooseParametersPart(Builder.With(x => x.Output = InputOutputJson));

                public ChooseOutputPart(TaskCreationInfo builder) : base(UICache.GetTasksInfoAsync().GetAwaiter().GetResult().WatchingOutputs, builder) =>
                    Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
            }
            class ChooseParametersPart : TaskCreationWindow.ParametersPart
            {
                public override TaskPart? Next => new LocalRemotePart(Builder.With(x => x.Data = Data));
                public ChooseParametersPart(TaskCreationInfo builder) : base(builder) { }
            }
            class LocalRemotePart : TaskCreationWindow.LocalRemoteTaskPart
            {
                public override TaskPart? Next => new EndPart(Builder.With(x => x.ExecuteLocally = Types.SelectedItem == TType.Local));
                public LocalRemotePart(TaskCreationInfo builder) : base(builder) { }
            }
            class EndPart : TaskPart
            {
                public override event Action<bool>? OnChoose;
                public override LocalizedString Title => new("End");
                public override TaskPart? Next => null;

                public EndPart(TaskCreationInfo builder) : base(builder)
                {
                    StartTaskAsync().Consume();
                    // TODO: start task
                    Children.Add(new TextBlock() { Text = "done thx" });
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
                    var post = await LocalApi.Post<string>(LocalApi.LocalIP, "startwatchingtask", new StringContent(serialized)).ConfigureAwait(false);
                    if (!post)
                    {
                        // Status($"error {post}");
                        return;
                    }
                }
            }
        }


        static class Settings
        {
            public static Setting Create(JProperty property, FieldDescriber describer) => describer.Nullable ? new NullableSetting(_Create(property, describer)) : _Create(property, describer);
            static Setting _Create(JProperty property, FieldDescriber describer) =>
                describer switch
                {
                    BooleanDescriber boo => new BoolSetting(boo, property),
                    StringDescriber txt => new TextSetting(txt, property),
                    NumberDescriber num => new NumberSetting(num, property),
                    ObjectDescriber obj => new ObjectSetting(obj, property),

                    _ => throw new InvalidOperationException($"Could not find setting type fot {describer.GetType().Name}"),
                };


            public class NamedControl : Grid
            {
                public NamedControl(string name, Control control)
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("Auto 20 *");
                    Children.Add(new TextBlock() { Text = name }.WithColumn(0));
                    Children.Add(control.WithColumn(2));
                }
            }

            public interface ISetting : IControl
            {
                JProperty Property { get; }
                new bool IsEnabled { get; set; }

                void UpdateValue();
            }
            public abstract class Setting : Panel, ISetting
            {
                public JProperty Property { get; }

                public Setting(JProperty property) => Property = property;

                protected void Set<TVal>(TVal value) where TVal : notnull => Property.Value = JValue.FromObject(value);
                protected JToken Get() => Property.Value;

                public abstract void UpdateValue();
            }
            abstract class Setting<T> : Setting, ISetting where T : FieldDescriber
            {
                protected readonly T Describer;

                public Setting(T describer, JProperty property) : base(property) => Describer = describer;
            }

            abstract class SettingContainer<T> : Setting<T> where T : FieldDescriber
            {
                public new T Describer => base.Describer;
                readonly Setting<T> Setting;

                protected SettingContainer(T describer, JProperty property) : base(describer, property) => Children.Add(Setting = CreateSetting());

                protected abstract Setting<T> CreateSetting();
                public sealed override void UpdateValue() => Setting.UpdateValue();
            }
            abstract class SettingChild<T> : Setting<T> where T : FieldDescriber
            {
                protected SettingChild(SettingContainer<T> parent) : base(parent.Describer, parent.Property) { }
            }

            class BoolSetting : Setting<BooleanDescriber>
            {
                readonly CheckBox Checkbox;

                public BoolSetting(BooleanDescriber describer, JProperty property) : base(describer, property)
                {
                    Checkbox = new CheckBox() { IsCancel = Get().Value<bool?>() ?? false };
                    Children.Add(Checkbox);
                }

                public override void UpdateValue() => Set(Checkbox.IsChecked == true);
            }
            class TextSetting : SettingContainer<StringDescriber>
            {
                public TextSetting(StringDescriber describer, JProperty property) : base(describer, property) { }

                protected override Setting<StringDescriber> CreateSetting()
                {
                    if (Describer.Attributes.OfType<LocalFileAttribute>().Any())
                        return new LocalFileSetting(this);
                    if (Describer.Attributes.OfType<LocalDirectoryAttribute>().Any())
                        return new LocalDirSetting(this);

                    return new TextBoxSetting(this);
                }


                class TextBoxSetting : SettingChild<StringDescriber>
                {
                    readonly TextBox TextBox;

                    public TextBoxSetting(TextSetting setting) : base(setting)
                    {
                        TextBox = new TextBox() { Text = Get().Value<string?>() ?? string.Empty };
                        Children.Add(TextBox);
                    }

                    public override void UpdateValue() => Set(TextBox.Text);
                }
                class LocalFileSetting : SettingChild<StringDescriber>
                {
                    string File = null!;

                    public LocalFileSetting(TextSetting setting) : base(setting)
                    {
                        var btn = new MPButton() { Text = new("Pick a file") };
                        btn.OnClick += () => new OpenFileDialog() { AllowMultiple = false }.ShowAsync((Window) VisualRoot!).ContinueWith(t => Dispatcher.UIThread.Post(() => btn.Text = new(File = t.Result!.First())));
                        Children.Add(btn);
                    }

                    public override void UpdateValue() => Set(File);
                }
                class LocalDirSetting : SettingChild<StringDescriber>
                {
                    string Dir = null!;

                    public LocalDirSetting(TextSetting setting) : base(setting)
                    {
                        var btn = new MPButton() { Text = new("Pick a directory") };
                        btn.OnClick += () => new OpenFolderDialog().ShowAsync((Window) VisualRoot!).ContinueWith(t => Dispatcher.UIThread.Post(() => btn.Text = new(Dir = t.Result!)));
                        Children.Add(btn);
                    }

                    public override void UpdateValue() => Set(Dir);
                }
            }
            class NumberSetting : SettingContainer<NumberDescriber>
            {
                public NumberSetting(NumberDescriber describer, JProperty property) : base(describer, property) { }

                protected override Setting<NumberDescriber> CreateSetting()
                {
                    var value = Get().Value<double?>() ?? 0;

                    var range = Describer.Attributes.OfType<RangedAttribute>().FirstOrDefault();
                    if (range is not null) return new SliderNumberSetting(this, range);

                    return new TextNumberSetting(this);
                }


                class SliderNumberSetting : SettingChild<NumberDescriber>
                {
                    readonly Slider Slider;

                    public SliderNumberSetting(NumberSetting setting, RangedAttribute range) : base(setting)
                    {
                        Slider = new Slider()
                        {
                            Minimum = range.Min,
                            Maximum = range.Max,
                            Orientation = Orientation.Horizontal,
                            Value = setting.Get().Value<double?>() ?? 0,
                        };

                        var valuetext = new TextBlock();
                        Slider.Subscribe(Slider.ValueProperty, v => valuetext.Text = v.ToString());

                        var grid = new Grid()
                        {
                            ColumnDefinitions = ColumnDefinitions.Parse("40 *"),
                            Children =
                            {
                                valuetext.WithColumn(0),
                                Slider.WithColumn(1),
                            },
                        };
                        Children.Add(grid);
                    }

                    public override void UpdateValue()
                    {
                        if (Describer.IsInteger) Set((long) Slider.Value);
                        else Set(Slider.Value);
                    }
                }
                class TextNumberSetting : SettingChild<NumberDescriber>
                {
                    readonly TextBox TextBox;

                    public TextNumberSetting(NumberSetting setting) : base(setting)
                    {
                        TextBox = new TextBox() { Text = setting.Get().Value<double?>()?.ToString() ?? "0" };
                        Children.Add(TextBox);

                        var isdouble = !setting.Describer.IsInteger;
                        TextBox.Subscribe(TextBox.TextProperty, text =>
                        {
                            text = Regex.Replace(text, isdouble ? @"[^0-9\.,]*" : @"[^0-9]*", string.Empty);
                            if (text.Length == 0) text = "0";
                        });
                    }

                    public override void UpdateValue()
                    {
                        if (Describer.IsInteger) Set(long.Parse(TextBox.Text));
                        else Set(double.Parse(TextBox.Text));
                    }
                }
            }

            class ObjectSetting : Setting
            {
                readonly List<ISetting> Settings = new();

                public ObjectSetting(ObjectDescriber describer, JObject jobj) : this(describer, new JProperty("___", jobj)) { }
                public ObjectSetting(ObjectDescriber describer, JProperty property) : base(property)
                {
                    Background = new SolidColorBrush(new Color(20, 0, 0, 0));
                    Margin = new Thickness(10, 0, 0, 0);

                    var list = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 10,
                    };
                    Children.Add(list);

                    var jobj = property.Value as JObject;
                    if (jobj is null || property.Value.Type == JTokenType.Null)
                        jobj = (JObject) (property.Value = new JObject());

                    foreach (var field in describer.Fields)
                    {
                        var jsonkey = field.Attributes.OfType<JsonPropertyAttribute>().FirstOrDefault()?.PropertyName ?? field.Name;
                        if (!jobj.ContainsKey(jsonkey))
                            jobj[jsonkey] = new JValue(field.DefaultValue);

                        var setting = Create(jobj.Property(jsonkey)!, field);
                        var control = new Settings.NamedControl(field.Name, setting);
                        list.Children.Add(control);
                        Settings.Add(setting);
                    }
                }

                public override void UpdateValue()
                {
                    foreach (var setting in Settings)
                        setting.UpdateValue();
                }
            }
            class NullableSetting : Setting, ISetting
            {
                readonly Setting Setting;
                readonly CheckBox EnabledCheckBox;

                public NullableSetting(Setting setting) : base(setting.Property)
                {
                    Setting = setting;

                    EnabledCheckBox = new CheckBox() { IsChecked = false, };
                    EnabledCheckBox.Subscribe(CheckBox.IsCheckedProperty, v =>
                    {
                        Setting.IsEnabled = v == true;
                        Setting.Opacity = v == true ? 1 : .5f;
                    });

                    var panel = new Grid()
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("Auto 20 *"),
                        Children =
                        {
                            EnabledCheckBox.WithColumn(0),
                            Setting.WithColumn(2),
                        },
                    };
                    Children.Add(panel);
                }

                public override void UpdateValue()
                {
                    if (EnabledCheckBox.IsChecked == true) Setting.UpdateValue();
                    else Property.Value = JValue.CreateNull();
                }
            }
        }
    }
}