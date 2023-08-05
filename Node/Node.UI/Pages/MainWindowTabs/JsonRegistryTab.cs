namespace Node.UI.Pages.MainWindowTabs;

public class JsonRegistryTab : Panel
{
    public JsonRegistryTab() => Reload().Consume();

    async Task Reload()
    {
        Children.Clear();

        var softlist = await Apis.Default.GetSoftwareAsync().ThrowIfError();
        var textbox = new TextBox()
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            Text = JsonConvert.SerializeObject(softlist, Formatting.Indented),
        };

        Children.Add(new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("* Auto"),
            Children =
            {
                textbox.WithRow(0),
                new MPButton()
                {
                    Text = "SAVE",
                    OnClickSelf = async self =>
                    {
                        // check if json is valid
                        JsonConvert.DeserializeObject<ImmutableDictionary<string, SoftwareDefinition>>(textbox.Text).ThrowIfNull();

                        using var content = new StringContent(textbox.Text) { Headers = { ContentType = new("application/json") } };

                        var result = await Api.Default.ApiPost<ImmutableDictionary<string, SoftwareDefinition>>($"{Apis.RegistryUrl}/editall", "value", "Edit registry", content)
                            .Next(x => x.WithComparers(StringComparer.OrdinalIgnoreCase).AsOpResult());
                        if (await self.FlashErrorIfErr(result))
                            return;

                        await Reload();
                    },
                }.WithRow(1),
            }
        });
    }
}
