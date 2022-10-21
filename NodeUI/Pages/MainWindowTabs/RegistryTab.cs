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
        var setting = TaskCreationWindow.Settings.Create(prop, FieldDescriber.Create(softlist.GetType()));
        Children.Add(new ScrollViewer() { Content = setting });
    }
}
