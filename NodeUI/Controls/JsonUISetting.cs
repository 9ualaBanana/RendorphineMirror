using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeUI.Controls;

public static class JsonUISetting
{
    public static Setting Create(JProperty property, FieldDescriber describer) => describer.Nullable ? new NullableSetting(_Create(property, describer)) : _Create(property, describer);
    static Setting _Create(JProperty property, FieldDescriber describer) =>
        describer switch
        {
            BooleanDescriber boo => new BoolSetting(boo, property),
            StringDescriber txt => new TextSetting(txt, property),
            NumberDescriber num => new NumberSetting(num, property),
            ObjectDescriber obj => new ObjectSetting(obj, property),
            EnumDescriber enm => new EnumSetting(enm, property),

            DictionaryDescriber dic when dic.KeyType == typeof(string) => new DictionarySetting(dic, property),
            CollectionDescriber col => new CollectionSetting(col, property),

            _ => throw new InvalidOperationException($"Could not find setting type fot {describer.GetType().Name}"),
        };


    public interface ISettingContainer : IEnumerable<Setting>
    {
        IEnumerable<Setting> GetTreeRecursive() => new[] { (Setting) this }.Concat(this.SelectMany(x => ((x as ISettingContainer) ?? Enumerable.Empty<Setting>()).Prepend(x)));
    }
    public abstract class Setting : Panel
    {
        public JProperty Property { get; }

        public Setting(JProperty property) => Property = property;

        protected void Set<TVal>(TVal value) where TVal : notnull => Property.Value = JValue.FromObject(value);
        protected JToken Get() => Property.Value;

        public abstract void UpdateValue();
    }
    abstract class Setting<T> : Setting where T : FieldDescriber
    {
        protected readonly T Describer;

        public Setting(T describer, JProperty property) : base(property) => Describer = describer;
    }

    abstract class SettingContainer<T> : Setting<T>, ISettingContainer where T : FieldDescriber
    {
        public new T Describer => base.Describer;
        readonly Setting<T> Setting;

        protected SettingContainer(T describer, JProperty property) : base(describer, property) => Children.Add(Setting = CreateSetting());

        protected abstract Setting<T> CreateSetting();
        public sealed override void UpdateValue() => Setting.UpdateValue();

        public IEnumerator<Setting> GetEnumerator() => Enumerable.Repeat(Setting, 1).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
    class DictionarySetting : Setting, ISettingContainer
    {
        readonly List<Setting> Settings = new();

        public DictionarySetting(DictionaryDescriber describer, JObject jobj) : this(describer, new JProperty("____", jobj)) { }
        public DictionarySetting(DictionaryDescriber describer, JProperty property) : base(property)
        {
            if (describer.KeyType != typeof(string)) throw new NotSupportedException("Non-string key dictionary describer is not supported");

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
                            var key = describer.DefaultKeyValue?.ToObject<string>() ?? "";
                            var basekey = key;
                            var value = describer.DefaultValueValue?.DeepClone();
                            if (value is null)
                            {
                                if (describer.ValueType == typeof(string)) value = "";
                                else value = new JObject();
                            }

                            int i = 0;
                            while (jobj.ContainsKey(key))
                                key = basekey + ++i;

                            jobj.Add(key, value);
                            recreate();
                        },
                    },
                    list,
                },
            };
            Children.Add(grid);

            recreate();


            void recreate()
            {
                Transitions ??= new();
                Transitions.Clear();
                Background = new SolidColorBrush(new Color(40, 0, 255, 0));
                Transitions.Add(new BrushTransition() { Property = BackgroundProperty, Duration = TimeSpan.FromSeconds(.5) });
                Background = new SolidColorBrush(new Color(20, 0, 0, 0));

                var openKeys = new List<string>();
                foreach (var expander in list.Children.OfType<Expander>())
                {
                    if (!expander.IsExpanded) continue;

                    var header = ((StackPanel) expander.Header!).Children.OfType<TextBox>().First().Text;
                    openKeys.Add(header);
                }

                Settings.Clear();
                list.Children.Clear();

                var fielddescriber = FieldDescriber.Create(describer.ValueType, describer.Attributes);
                foreach (var property in jobj.Properties().OrderBy(x => x.Name))
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

                    var keyTextBox = new TextBox() { Text = key };
                    var expander = (null as Expander)!;

                    list.Children.Add(expander = new Expander()
                    {
                        IsExpanded = openKeys.Contains(key),
                        Header = new StackPanel()
                        {
                            Orientation = Orientation.Horizontal,
                            Children =
                            {
                                keyTextBox,
                                new MPButton()
                                {
                                    Text = "Set key",
                                    OnClick = () =>
                                    {
                                        var parent = (JObject) property.Parent!;
                                        parent.Remove(property.Name);
                                        parent.Add(keyTextBox.Text.Trim(), property.Value);

                                        recreate();
                                    },
                                },
                            },
                        },
                        Content = set,
                    });
                    Settings.Add(setting);
                }

                openKeys.Clear();
            }
        }

        public override void UpdateValue()
        {
            foreach (var setting in Settings)
                setting.UpdateValue();
        }


        public IEnumerator<Setting> GetEnumerator() => Settings.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    class CollectionSetting : Setting, ISettingContainer
    {
        readonly List<Setting> Settings = new();

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


        public IEnumerator<Setting> GetEnumerator() => Settings.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    class EnumSetting : Setting<EnumDescriber>
    {
        public EnumSetting(EnumDescriber describer, JProperty property) : base(describer, property)
        {

        }

        public override void UpdateValue() { }
    }

    class ObjectSetting : Setting, ISettingContainer
    {
        readonly List<Setting> Settings = new();

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
                if (field.Attributes.OfType<HiddenAttribute>().Any()) continue;

                var jsonkey = field.Attributes.OfType<JsonPropertyAttribute>().FirstOrDefault()?.PropertyName ?? field.Name;
                if (!jobj.ContainsKey(jsonkey))
                    jobj[jsonkey] = field.DefaultValue;

                var setting = Create(jobj.Property(jsonkey)!, field);
                list.Children.Add(new Grid()
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("Auto 20 *"),
                    Children =
                    {
                        new TextBlock() { Text = field.Name }.WithColumn(0),
                        setting.WithColumn(2),
                    }
                });

                Settings.Add(setting);
            }
        }

        public override void UpdateValue()
        {
            foreach (var setting in Settings)
                setting.UpdateValue();
        }


        public IEnumerator<Setting> GetEnumerator() => Settings.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    class NullableSetting : Setting
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