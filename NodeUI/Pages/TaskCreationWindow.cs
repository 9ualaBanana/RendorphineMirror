using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Controls.Templates;
using Common.Tasks;
using Common.Tasks.Tasks;
using Common.Tasks.Tasks.DTO;
using Newtonsoft.Json;

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

            Content = new TaskPartContainer();
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

        static FuncDataTemplate<T> CreateTemplate<T>(Func<T, IControl> func) => new((t, _) => func(t));
        static ListBox CreateListBox<T>(IReadOnlyCollection<T> items, Func<T, IControl> func) =>
            new ListBox()
            {
                Items = items,
                ItemTemplate = CreateTemplate<T>(func),
            };


        class TaskPartContainer : Grid
        {
            readonly TextBlock TitleTextBlock;
            readonly MPButton BackButton, NextButton;
            readonly Stack<TaskPart> Parts = new();

            public TaskPartContainer()
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

                // ShowPart(new ChoosePluginPart());


                bool set = false;
                GlobalState.SoftwareStats.SubscribeChanged(z, true);

                void z(ImmutableDictionary<PluginType, Api.SoftwareStats> oldv, ImmutableDictionary<PluginType, Api.SoftwareStats> newv)
                {
                    if (newv.IsEmpty) return;
                    if (set) return;
                    set = true;

                    Dispatcher.UIThread.Post(() => ShowPart(new ParametersPart(TaskList.TryGet("EditRaster")!, newv[PluginType.FFmpeg].ByVersion.Keys.First(), new Uploadp("610a371c6e60182b1ea29c97", "3_UGVlayAyMDIxLTA4LTA0IDEzLTI5", 210210))));
                }
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

            public virtual void OnNext() { }
        }
        class ChoosePluginPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Plugin");
            public override TaskPart? Next => new ChooseVersionPart((PluginType) PluginsList.SelectedItem!);

            readonly ListBox PluginsList;

            public ChoosePluginPart()
            {
                PluginsList = CreateListBox(TaskList.Types, PluginToControl);
                PluginsList.SelectionChanged += (obj, e) =>
                    OnChoose?.Invoke(PluginsList.SelectedItems.Count != 0);

                Children.Add(PluginsList);
            }

            Control PluginToControl(PluginType type) =>
                new Grid()
                {
                    RowDefinitions = RowDefinitions.Parse("Auto"),
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = type.ToString(),
                        }.WithRow(0),
                    },
                };
        }
        class ChooseVersionPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new($"Choose {Type.GetName()} Version");
            public override TaskPart? Next => new ChooseActionPart(Type, (string) VersionsList.SelectedItem!);

            readonly PluginType Type;
            readonly ListBox VersionsList;

            public ChooseVersionPart(PluginType type)
            {
                Type = type;

                string[] versions;
                if (GlobalState.SoftwareStats.Value.TryGetValue(type, out var stats))
                    versions = stats.ByVersion.Keys.ToArray();
                else versions = Array.Empty<string>();

                VersionsList = CreateListBox(versions, VersionToControl);
                VersionsList.SelectionChanged += (obj, e) =>
                    OnChoose?.Invoke(VersionsList.SelectedItems.Count != 0);

                Children.Add(VersionsList);
            }

            Control VersionToControl(string version) =>
                new Grid()
                {
                    RowDefinitions = RowDefinitions.Parse("Auto"),
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = version,
                        }.WithRow(0),
                    },
                };
        }
        class ChooseActionPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Action");
            public override TaskPart? Next => new ChooseFilesPart((IPluginAction) ActionsList.SelectedItem!, Version);

            readonly ListBox ActionsList;
            readonly string Version;

            public ChooseActionPart(PluginType type, string version)
            {
                Version = version;

                ActionsList = CreateListBox(TaskList.Get(type).ToArray(), ActionToControl);
                ActionsList.SelectionChanged += (obj, e) =>
                    OnChoose?.Invoke(ActionsList.SelectedItems.Count != 0);

                Children.Add(ActionsList);
            }

            Control ActionToControl(IPluginAction action) =>
                new Grid()
                {
                    RowDefinitions = RowDefinitions.Parse("Auto"),
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = action.Name,
                        }.WithRow(0),
                    },
                };
        }
        class ChooseFilesPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Version");
            public override TaskPart? Next => new ParametersPart(Action, Version, new Uploadp(IidInput.Text, NameInput.Text, long.Parse(SizeInput.Text)));

            readonly IPluginAction Action;
            readonly string Version;
            readonly TextBox IidInput, NameInput, SizeInput;

            public ChooseFilesPart(IPluginAction action, string version)
            {
                Action = action;
                Version = version;

                IidInput = new TextBox()
                {
                    Text = "fileiid",
                };
                NameInput = new TextBox()
                {
                    Text = "filename",
                };
                SizeInput = new TextBox()
                {
                    Text = "filesize",
                };

                var panel = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children = { IidInput, NameInput, SizeInput },
                };
                Children.Add(panel);
            }
        }
        class ParametersPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Modify Parameters");
            public override TaskPart? Next => new ChooseOutputDirPart(Action, Data, Version, Upload);

            readonly IPluginAction Action;
            IPluginActionData Data = null!;
            readonly string Version;
            readonly Uploadp Upload;
            readonly StackPanel List;

            public ParametersPart(IPluginAction action, string version, Uploadp upload)
            {
                Action = action;
                Version = version;
                Upload = upload;

                List = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                };
                Children.Add(List);

                initAsync().Consume();


                async Task initAsync()
                {
                    Data = await action.CreateData().ConfigureAwait(false);

                    foreach (var property in GetProperties(Data.GetType()))
                        await Dispatcher.UIThread.InvokeAsync(() => CreateConfigs(List, Data, property)).ConfigureAwait(false);

                    Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
                }
            }

            public override void OnNext()
            {
                base.OnNext();

                foreach (var setting in List.Children.OfType<Setting>())
                    setting.UpdateValue();
            }


            static IEnumerable<PropertyInfo> GetProperties(Type type) =>
                type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x is FieldInfo or System.Reflection.PropertyInfo)
                .Select(x => (PropertyInfo) x);


            static void CreateConfigs(Panel list, object data, PropertyInfo property)
            {
                var setting = CreateSetting(property.PropertyType, data, property);

                if (setting is not null) list.Children.Add(setting);
                else Log.Error("Could not find setting control for the type " + property.PropertyType.Name);
            }
            static Setting? CreateSetting(Type type, object data, PropertyInfo property)
            {
                if (property.IsNullable())
                {
                    var setting = CreateSettingNotNull(Nullable.GetUnderlyingType(type) ?? type, data, property);
                    if (setting is null) return null;

                    return new NullableSetting(setting, data, property);
                }

                return CreateSettingNotNull(type, data, property);
            }
            static Setting? CreateSettingNotNull(Type type, object data, PropertyInfo property)
            {
                if (istype<bool>()) return new BoolSetting(data, property);
                else if (istype<string>()) return new TextSetting(data, property);
                else if (type.GetInterfaces().Any(x => x.Name.StartsWith("INumber", StringComparison.Ordinal)))
                    return new NumberSetting(data, property);
                else if (property.PropertyType.IsClass) return new ObjectSetting(data, property);

                return null;


                bool istype<T>() => type == typeof(T);
            }


            readonly struct PropertyInfo
            {
                public Type PropertyType => Nullable.GetUnderlyingType(_PropertyType) ?? _PropertyType;
                Type _PropertyType => Property?.PropertyType ?? Field?.FieldType!;

                public string Name => Property?.Name ?? Field?.Name!;

                readonly System.Reflection.PropertyInfo? Property;
                readonly FieldInfo? Field;

                public PropertyInfo(FieldInfo field)
                {
                    Field = field;
                    Property = null;
                }
                public PropertyInfo(System.Reflection.PropertyInfo property)
                {
                    Property = property;
                    Field = null;
                }

                public void SetValue(object? obj, object? value)
                {
                    if (obj is null) return;

                    Property?.SetValue(obj, value);
                    Field?.SetValue(obj, value);
                }
                public object? GetValue(object? obj) => obj is null ? null : Property?.GetValue(obj) ?? Field?.GetValue(obj)!;

                public T? GetAttribute<T>() where T : Attribute => Property?.GetCustomAttribute<T>() ?? Field?.GetCustomAttribute<T>();
                public bool IsNullable() =>
                    Property is not null ? new NullabilityInfoContext().Create(Property).WriteState is NullabilityState.Nullable
                    : Field is not null ? new NullabilityInfoContext().Create(Field).WriteState is NullabilityState.Nullable
                    : false;

                public static implicit operator PropertyInfo(MemberInfo member) =>
                    member is System.Reflection.PropertyInfo p ? new(p)
                    : member is FieldInfo f ? new(f)
                    : throw new InvalidOperationException();
            }
            abstract class Setting : StackPanel
            {
                protected readonly object Data;
                protected readonly PropertyInfo Property;

                public Setting(object data, PropertyInfo property)
                {
                    Data = data;
                    Property = property;

                    Orientation = Orientation.Horizontal;
                    Spacing = 10;
                    Children.Add(new TextBlock() { Text = property.Name, });
                }

                protected void Set<T>(T value) => Property.SetValue(Data, value);
                protected void Set(object value) => Property.SetValue(Data, value);
                protected object? Get() => Property.GetValue(Data);

                public abstract void UpdateValue();
            }
            class BoolSetting : Setting
            {
                readonly CheckBox Checkbox;

                public BoolSetting(object data, PropertyInfo property) : base(data, property)
                {
                    Checkbox = new CheckBox()
                    {
                        IsCancel = Get() is bool b && b,
                    };
                    Children.Add(Checkbox);
                }

                public override void UpdateValue() => Set(Checkbox.IsChecked == true);
            }
            class TextSetting : Setting
            {
                readonly TextBox TextBox;

                public TextSetting(object data, PropertyInfo property) : base(data, property)
                {
                    TextBox = new TextBox()
                    {
                        Text = Get()?.ToString() ?? string.Empty,
                    };
                    Children.Add(TextBox);
                }

                public override void UpdateValue() => Set(TextBox.Text);
            }
            class NumberSetting : Setting
            {
                readonly TextBox TextBox;

                public NumberSetting(object data, PropertyInfo property) : base(data, property)
                {
                    TextBox = new TextBox()
                    {
                        Text = Get()?.ToString() ?? "0",
                    };
                    Children.Add(TextBox);

                    var isdouble = property.PropertyType.GetInterfaces().Any(x => x.Name.StartsWith("IFloatingPoint", StringComparison.Ordinal));
                    TextBox.Subscribe(TextBox.TextProperty, text =>
                    {
                        text = Regex.Replace(text, isdouble ? @"[^0-9\.,]*" : @"[^0-9]*", string.Empty);
                        if (text.Length == 0) text = "0";
                    });
                }

                public override void UpdateValue()
                {
                    var parse = Property.PropertyType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string), typeof(IFormatProvider) })!;
                    Set(parse.Invoke(null, new object[] { TextBox.Text, CultureInfo.InvariantCulture })!);
                }
            }

            class ObjectSetting : Setting
            {
                readonly StackPanel Settings;

                public ObjectSetting(object data, PropertyInfo property) : base(data, property)
                {
                    Background = new SolidColorBrush(new Color(20, 0, 0, 0));
                    Margin = new Thickness(10, 0, 0, 0);

                    Settings = new StackPanel()
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock() { Text = property.PropertyType.Name },
                        },
                    };
                    Children.Add(Settings);

                    var self = property.GetValue(data);
                    if (self is null)
                    {
                        self = Activator.CreateInstance(property.PropertyType)!;
                        property.SetValue(data, self);
                    }

                    foreach (var prop in GetProperties(property.PropertyType))
                        CreateConfigs(Settings, self, prop);
                }

                public override void UpdateValue()
                {
                    foreach (var setting in Settings.Children.OfType<Setting>())
                        setting.UpdateValue();
                }
            }
            class NullableSetting : Setting
            {
                readonly Setting Setting;
                readonly CheckBox EnabledCheckBox;

                public NullableSetting(Setting setting, object data, PropertyInfo property) : base(data, property)
                {
                    Setting = setting;

                    EnabledCheckBox = new CheckBox()
                    {
                        IsChecked = !property.IsNullable() && property.GetValue(data) is not null,
                    };
                    EnabledCheckBox.Subscribe(CheckBox.IsCheckedProperty, v =>
                    {
                        Setting.IsEnabled = v == true;
                        Setting.Opacity = v == true ? 1 : .5f;
                    });

                    var panel = new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 10,
                        Children =
                        {
                            EnabledCheckBox,
                            Setting,
                        },
                    };
                    Children.Add(panel);
                }

                public override void UpdateValue()
                {
                    if (EnabledCheckBox.IsChecked == true) Setting.UpdateValue();
                    else Property.SetValue(Data, null);
                }
            }
        }
        class ChooseOutputDirPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Choose Output Directory");
            public override TaskPart? Next => new WaitingPart(Action, Data, Upload, DirInput.Text, FilenameInput.Text);

            readonly IPluginAction Action;
            readonly IPluginActionData Data;
            readonly string Version;
            readonly Uploadp Upload;

            readonly TextBox DirInput, FilenameInput;

            public ChooseOutputDirPart(IPluginAction action, IPluginActionData data, string version, Uploadp upload)
            {
                Action = action;
                Data = data;
                Version = version;
                Upload = upload;

                DirInput = new TextBox()
                {
                    Text = "output_dir",
                };
                FilenameInput = new TextBox()
                {
                    Text = "output_filename",
                };

                var panel = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Children =
                    {
                        DirInput,
                        FilenameInput,
                    }
                };
                Children.Add(panel);

                Dispatcher.UIThread.Post(() => OnChoose?.Invoke(true));
            }
        }
        class WaitingPart : TaskPart
        {
            public override event Action<bool>? OnChoose;
            public override LocalizedString Title => new("Waiting");
            public override TaskPart? Next => null;

            readonly IPluginAction Action;
            readonly IPluginActionData Data;
            readonly Uploadp Upload;
            readonly string OutputDir, OutputFilename;

            readonly TextBlock StatusTextBlock;

            public WaitingPart(IPluginAction action, IPluginActionData data, Uploadp upload, string outputdir, string outputFilename)
            {
                Action = action;
                Data = data;
                Upload = upload;
                OutputDir = outputdir;
                OutputFilename = outputFilename;

                Children.Add(StatusTextBlock = new TextBlock());
                _ = StartTaskAsync();
            }

            string Status() => @$"
                waiting {Action.Name}
                with {Action.Type}
                on {string.Join(", ", Upload)}
                to {OutputDir}
                and {JsonConvert.SerializeObject(Data)}
                ".TrimLines();
            void Status(string? text) => Dispatcher.UIThread.Post(() => StatusTextBlock.Text = text + Environment.NewLine + Status());

            async ValueTask StartTaskAsync()
            {
                var jcontent = new StringContent(JsonConvert.SerializeObject(new
                {
                    upload = Upload,
                    data = (object) Data,
                    outputdir = OutputDir,
                    outputfilename = OutputFilename,
                }, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto }));

                var post = await LocalApi.JustPost(LocalApi.LocalIP, "starttask", jcontent).ConfigureAwait(false);
                var jreader = new JsonTextReader(new StreamReader(await post.Content.ReadAsStreamAsync().ConfigureAwait(false))) { SupportMultipleContent = true };
                var jserializer = new Newtonsoft.Json.JsonSerializer();

                var stat = "\n";
                Status(stat + "zipping...");
                await jreader.ReadAsync().ConfigureAwait(false);
                var zip = jserializer.Deserialize<OperationResult<string>>(jreader).Value;
                stat += $"zip: {zip}\n";

                Status(stat + "uploading...");
                await jreader.ReadAsync().ConfigureAwait(false);
                var upload = jserializer.Deserialize<OperationResult<Uploadp>>(jreader).Value;
                stat += $"fileid: {upload.FileId}\n";

                Status(stat + "creating task...");
                await jreader.ReadAsync().ConfigureAwait(false);
                var taskid = jserializer.Deserialize<OperationResult<string>>(jreader).Value;
                stat += $"taskid: {taskid}\n";
            }
        }

        record Uploadp(string FileId, string FileName, long UploadedBytesCount);
    }
}