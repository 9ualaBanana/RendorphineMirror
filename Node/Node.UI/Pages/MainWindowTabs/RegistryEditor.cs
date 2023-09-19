namespace Node.UI.Pages.MainWindowTabs;

public class RegistryEditor : Panel
{
    public RegistryEditor(NodeGlobalState state)
    {
        var pluginselector = new PluginVersionSelector(state.Software);

        Children.Add(new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("Auto *"),
            Children =
            {
                pluginselector.WithRow(0),
                new MPButton()
                {
                    Text = "Reload",
                    OnClickSelf = async self =>
                    {
                        var result = await App.Instance.SoftwareUpdater.Update(state.Software);
                        await self.FlashErrorIfErr(result);
                    },
                }.WithRow(1),
            },
        });
    }
}
