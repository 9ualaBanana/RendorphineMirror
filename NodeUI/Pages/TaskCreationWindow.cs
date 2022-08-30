using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Controls.Templates;
using Common.Tasks.Model;
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
                string[] versions;
                if (UICache.SoftwareStats.Value.TryGetValue(Builder.Type, out var stats))
                    versions = stats.ByVersion.Keys.ToArray();
                else versions = Array.Empty<string>();

                var list = CreateListBox(versions.Prepend(AnyVersion).ToArray(), version => new TextBlock() { Text = version });
                list.SelectionChanged += (obj, e) =>
                {
                    Builder.Version = list.SelectedItem == AnyVersion ? null : list.SelectedItem;
                    OnChoose?.Invoke(list.SelectedItems.Count != 0);
                };

                Children.Add(list);
            }
        }
        abstract class ChooseActionPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Action");

            public ChooseActionPart(TaskCreationInfo builder) : base(builder) { }

            public override void Initialize()
            {
                var list = CreateListBox(NodeGlobalState.Instance.TaskDefinitions.Value.Actions.Where(x => x.Type == Builder.Type).ToArray(), action => new TextBlock() { Text = action.Name });
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
            Settings.ISetting Setting = null!;

            public ChooseInputOutputPart(TaskCreationInfo builder) : base(builder) { }

            protected void InitFromCache(Func<TasksFullDescriber, ImmutableArray<TaskInputOutputDescriber>> func) => Init(func(NodeGlobalState.Instance.TaskDefinitions.Value));
            protected void Init(IReadOnlyList<TaskInputOutputDescriber> describers) => Init(describers, t => new TextBlock() { Text = t.Type });

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

            Settings.ISetting Setting = null!;

            public ParametersPart(TaskCreationInfo builder) : base(builder) { }

            public override void Initialize()
            {
                var describer = NodeGlobalState.Instance.TaskDefinitions.Value.Actions.First(x => x.Name == Builder.Action).DataDescriber;
                var data = Builder.Data = new JObject();

                data.RemoveAll();
                data["type"] = Builder.Action;

                var parent = new JObject() { [describer.Name] = data, };
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
                var taskid = await LocalApi.Post<string>(LocalApi.LocalIP, StartTaskEndpoint, new StringContent(serialized)).ConfigureAwait(false);
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
                protected override string StartTaskEndpoint => "starttask";

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
                protected override string StartTaskEndpoint => "startwatchingtask";

                public EndPart(TaskCreationInfo builder) : base(builder) { }
            }
        }


        public static class Settings
        {
            public static Setting Create(JProperty property, FieldDescriber describer) => describer.Nullable ? new NullableSetting(_Create(property, describer)) : _Create(property, describer);
            static Setting _Create(JProperty property, FieldDescriber describer) =>
                describer switch
                {
                    BooleanDescriber boo => new BoolSetting(boo, property),
                    StringDescriber txt => new TextSetting(txt, property),
                    NumberDescriber num => new NumberSetting(num, property),
                    ObjectDescriber obj => new ObjectSetting(obj, property),

                    DictionaryDescriber dic => new DictionarySetting(dic, property),
                    CollectionDescriber col => new CollectionSetting(col, property),

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
                        var textinput = new TextBox();
                        textinput.Subscribe(TextBox.TextProperty, text => File = text);

                        var btn = new MPButton() { Text = new("Pick a file") };
                        btn.OnClick += () => new OpenFileDialog() { AllowMultiple = false }.ShowAsync((Window) VisualRoot!).ContinueWith(t => Dispatcher.UIThread.Post(() => textinput.Text = new(t.Result?.FirstOrDefault() ?? string.Empty)));

                        var grid = new Grid()
                        {
                            ColumnDefinitions = ColumnDefinitions.Parse("8* 2*"),
                            Children = { textinput.WithColumn(0), btn.WithColumn(1), },
                        };
                        Children.Add(grid);
                    }

                    public override void UpdateValue() => Set(File);
                }
                class LocalDirSetting : SettingChild<StringDescriber>
                {
                    string Dir = null!;

                    public LocalDirSetting(TextSetting setting) : base(setting)
                    {
                        var textinput = new TextBox();
                        textinput.Subscribe(TextBox.TextProperty, text => Dir = text);

                        var btn = new MPButton() { Text = new("Pick a directory") };
                        btn.OnClick += () => new OpenFolderDialog().ShowAsync((Window) VisualRoot!).ContinueWith(t => Dispatcher.UIThread.Post(() => textinput.Text = new(t.Result ?? string.Empty)));

                        var grid = new Grid()
                        {
                            ColumnDefinitions = ColumnDefinitions.Parse("8* 2*"),
                            Children = { textinput.WithColumn(0), btn.WithColumn(1), },
                        };
                        Children.Add(grid);
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

                        if (setting.Describer.IsInteger)
                        {
                            Slider.TickFrequency = 1;
                            Slider.IsSnapToTickEnabled = true;
                        }

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
                        if (Describer.IsInteger) Set(long.Parse(TextBox.Text, CultureInfo.InvariantCulture));
                        else Set(double.Parse(TextBox.Text, CultureInfo.InvariantCulture));
                    }
                }
            }
            class DictionarySetting : Setting
            {
                readonly List<ISetting> Settings = new();

                public DictionarySetting(DictionaryDescriber describer, JObject jobj) : this(describer, new JProperty("____", jobj)) { }
                public DictionarySetting(DictionaryDescriber describer, JProperty property) : base(property)
                {
                    Background = new SolidColorBrush(new Color(20, 0, 0, 0));
                    Margin = new Thickness(10, 0, 0, 0);

                    var jobj = property.Value as JObject;
                    if (jobj is null) property.Value = jobj = new JObject();

                    var list = new StackPanel() { Orientation = Orientation.Vertical };
                    var grid = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 10,
                        Children =
                        {
                            new MPButton()
                            {
                                Text = "+ add",
                                OnClick = () =>
                                {
                                    jobj.Add(JsonConvert.SerializeObject(JToken.FromObject(toobj(describer.KeyType)!)), JToken.FromObject(toobj(describer.ValueType)!));
                                    recreate();


                                    static object? toobj(Type type)
                                    {
                                        if (type == typeof(string)) return "";
                                        return new JObject().ToObject(type)!;
                                    }
                                },
                            },
                            list,
                        },
                    };
                    Children.Add(grid);
                    recreate();


                    void recreate()
                    {
                        Settings.Clear();
                        list.Children.Clear();

                        var fielddescriber = FieldDescriber.Create(describer.ValueType, describer.Attributes);
                        foreach (var property in jobj.Properties())
                        {
                            var key = property.Name;
                            var value = property.Value;

                            var setting = Create(property, fielddescriber);
                            var set = new StackPanel()
                            {
                                Orientation = Orientation.Vertical,
                                Spacing = 10,
                                Children =
                                {
                                    new MPButton()
                                    {
                                        Text = "- delete",
                                        OnClick = () =>
                                        {
                                            jobj.Remove(key);
                                            recreate();
                                        },
                                    },
                                    setting,
                                },
                            };

                            list.Children.Add(new Expander() { Header = key, Content = set, });
                            Settings.Add(setting);
                        }
                    }
                }

                public override void UpdateValue()
                {
                    foreach (var setting in Settings)
                        setting.UpdateValue();
                }
            }
            class CollectionSetting : Setting
            {
                readonly List<ISetting> Settings = new();

                public CollectionSetting(CollectionDescriber describer, JArray jarr) : this(describer, new JProperty("____", jarr)) { }
                public CollectionSetting(CollectionDescriber describer, JProperty property) : base(property)
                {
                    Background = new SolidColorBrush(new Color(20, 0, 0, 0));
                    Margin = new Thickness(10, 0, 0, 0);

                    var jarr = property.Value as JArray;
                    if (jarr is null) property.Value = jarr = new JArray();

                    var list = new StackPanel() { Orientation = Orientation.Vertical };

                    var addbtn = new MPButton()
                    {
                        Text = "+ add",
                        OnClick = () =>
                        {
                            jarr.Add(toobj(describer.ValueType)!);
                            recreate();

                            static object? toobj(Type type)
                            {
                                if (type == typeof(string)) return "";

                                try { return Activator.CreateInstance(type)!; }
                                catch { }

                                return new JObject().ToObject(type)!;
                            }
                        },
                    };

                    var grid = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 10,
                        Children = { addbtn, list },
                    };
                    Children.Add(grid);
                    recreate();


                    void recreate()
                    {
                        Settings.Clear();
                        list.Children.Clear();

                        var fielddescriber = FieldDescriber.Create(describer.ValueType, describer.Attributes);
                        for (int i = 0; i < jarr.Count; i++)
                        {
                            var value = jarr[i];

                            var setting = Create(new JProperty("_" + i, value), fielddescriber);
                            var set = new StackPanel()
                            {
                                Orientation = Orientation.Vertical,
                                Spacing = 10,
                                Children =
                                {
                                    new MPButton()
                                    {
                                        Text = "- delete",
                                        OnClick = () =>
                                        {
                                            jarr.Remove(value);
                                            recreate();
                                        },
                                    },
                                    setting,
                                },
                            };

                            list.Children.Add(new Expander() { Header = i.ToString(), Content = set, });
                            Settings.Add(setting);
                        }
                    }
                }

                public override void UpdateValue()
                {
                    int index = 0;
                    foreach (var setting in Settings)
                    {
                        setting.UpdateValue();
                        ((JArray) Property.Value)[index++] = setting.Property.Value;
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