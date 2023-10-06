namespace Node.UI.Controls;

public class PluginVersionSelector : Panel
{
    public bool IsSelected => Plugins.SelectedIndex != -1 && Versions.SelectedIndex != -1;

    public IReadOnlyBindable<(PluginType type, PluginVersion version)> Selected => _Selected;
    readonly Bindable<(PluginType type, PluginVersion version)> _Selected;

    readonly IReadOnlyBindable<ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>> Software;

    readonly TypedComboBox<PluginType> Plugins;
    readonly TypedComboBox<PluginVersion> Versions;

    public PluginVersionSelector(IReadOnlyBindable<ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>> software)
    {
        Software = software = software.GetBoundCopy();
        _Selected = new();

        Versions = TypedComboBox.Create(Array.Empty<PluginVersion>()).With(c => c.MinWidth = 100);
        Plugins = TypedComboBox.Create(Array.Empty<PluginType>()).With(c => c.MinWidth = 100);

        Plugins.SelectionChanged += (obj, e) =>
        {
            Versions.Items = Software.Value.GetValueOrDefault(Plugins.SelectedItem)?.Keys.ToArray() ?? Array.Empty<PluginVersion>();
        };
        Versions.SelectionChanged += (obj, e) => _Selected.Value = (Plugins.SelectedItem, Versions.SelectedItem);

        Software.SubscribeChanged(() => Dispatcher.UIThread.Post(() => Plugins.Items = Software.Value.Keys.ToArray()), true);

        Children.Add(new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto Auto"),
            RowDefinitions = RowDefinitions.Parse("Auto Auto"),
            Children =
            {
                new TextBlock().Bind("Plugin").WithRowColumn(0, 0),
                new TextBlock().Bind("Version").WithRowColumn(0, 1),
                Plugins.WithRowColumn(1, 0),
                Versions.WithRowColumn(1, 1),
            },
        });
    }
}
