using static Node.UI.Controls.JsonUISetting;

namespace Node.UI.Controls;

public class JsonEditorList
{
    public static readonly JsonEditorList Default = new(ImmutableArray<Func<FieldDescriber, JProperty, JsonEditorList, Setting?>>.Empty);

    readonly ImmutableArray<Func<FieldDescriber, JProperty, JsonEditorList, Setting?>> SettingFuncs;

    public JsonEditorList(ImmutableArray<Func<FieldDescriber, JProperty, JsonEditorList, Setting?>> settingFuncs) => SettingFuncs = settingFuncs;

    public Setting Create(JProperty property, FieldDescriber describer) =>
        describer.Nullable ? new NullableSetting(_Create(property, describer), this) : _Create(property, describer);

    Setting _Create(JProperty property, FieldDescriber describer) =>
        describer switch
        {
            { } when SettingFuncs
                .Select(f => f(describer, property, this))
                .WhereNotNull()
                .FirstOrDefault() is { } setting => setting,

            BooleanDescriber boo => new BoolSetting(boo, property, this),
            StringDescriber txt => new TextSetting(txt, property, this),
            NumberDescriber num => new NumberSetting(num, property, this),
            ObjectDescriber obj => new ObjectSetting(obj, property, this),
            EnumDescriber enm => new EnumSetting(enm, property, this),

            DictionaryDescriber dic => new DictionarySetting(dic, property, this),
            CollectionDescriber col => new CollectionSetting(col, property, this),

            _ => throw new InvalidOperationException($"Could not find setting type for {describer?.GetType().Name}"),
        };
}
