using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NodeUI.Pages.MainWindowTabs;

public class RegistryTab : Panel
{
    public RegistryTab() => Reload().Consume();

    async Task Reload()
    {
        Children.Clear();

        var softlist = (await Apis.GetSoftwareAsync()).ThrowIfError();
        var prop = new JProperty("_", JObject.FromObject(softlist, JsonSettings.LowercaseS));

        var describer = new DictionaryDescriber(softlist.GetType())
        {
            DefaultKeyValue = JToken.FromObject("NewSoft"),
            DefaultValueValue = JToken.FromObject(new SoftwareDefinition(
                "New Mega Super Software",
                new Dictionary<string, SoftwareVersionDefinition>() { ["0.1"] = new SoftwareVersionDefinition("") }.ToImmutableDictionary(),
                null,
                ImmutableArray<string>.Empty
            ), JsonSettings.LowercaseS)
        };
        var setting = JsonUISetting.Create(prop, describer);


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
                        var stream = new MemoryStream();
                        using (var writer = new JsonTextWriter(new StreamWriter(stream, leaveOpen: true)))
                            await ((JObject) prop.Value).WriteToAsync(writer);

                        stream.Position = 0;
                        using var content = new StreamContent(stream) { Headers = { ContentType = new("application/json") } };

                        var result = await LocalApi.Post<ImmutableDictionary<string, SoftwareDefinition>>(Settings.RegistryUrl, "editall", content)
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
