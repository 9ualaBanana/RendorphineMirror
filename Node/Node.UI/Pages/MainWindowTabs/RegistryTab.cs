using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.UI.Pages.MainWindowTabs;

public class RegistryTab : Panel
{
    public RegistryTab() => Reload().Consume();

    async Task Reload()
    {
        Children.Clear();

        var softlist = await Apis.Default.GetSoftwareAsync().ThrowIfError();
        var describer = new DictionaryDescriber(softlist.GetType())
        {
            DefaultKeyValue = JToken.FromObject("NewSoft"),
            DefaultValueValue = JToken.FromObject(new SoftwareDefinition(
                "New Mega Super Software",
                new Dictionary<PluginVersion, SoftwareVersionDefinition>()
                {
                    ["0.1"] = new SoftwareVersionDefinition(
                        new SoftwareInstallation(),
                        new SoftwareRequirements(ImmutableDictionary<PlatformID, SoftwareSupportedPlatform>.Empty,
                        ImmutableArray<SoftwareParent>.Empty))
                }.ToImmutableDictionary()
            ), JsonSettings.LowercaseS)
        };
        var setting = JsonUISetting.Create(new JProperty("_", JObject.FromObject(softlist, JsonSettings.LowercaseS)), describer);


        Children.Add(new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Spacing = 20,
            Children =
            {
                new MPButton()
                {
                    Text = "SAVE",
                    OnClickSelf = async self =>
                    {
                        setting.UpdateValue();

                        var stream = new MemoryStream();
                        using (var writer = new JsonTextWriter(new StreamWriter(stream, leaveOpen: true)))
                            await ((JObject) setting.Property.Value).WriteToAsync(writer);

                        stream.Position = 0;
                        using var content = new StreamContent(stream) { Headers = { ContentType = new("application/json") } };

                        var result = await Api.Default.ApiPost<ImmutableDictionary<string, SoftwareDefinition>>($"{Apis.RegistryUrl}/editall", "value", "Edit registry", content)
                            .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());
                        if (await self.FlashErrorIfErr(result))
                            return;

                        await Reload();
                    },
                },
                new ScrollViewer() { Content = setting }
            },
        });
    }
}
