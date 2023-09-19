namespace Node.UI.Controls;

public class PluginVersionSelector : Panel
{
    public IReadOnlyBindable<(PluginType type, PluginVersion version)> Selected => _Selected;
    readonly Bindable<(PluginType type, PluginVersion version)> _Selected;

    readonly IReadOnlyBindable<ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>> Software;

    public PluginVersionSelector(IReadOnlyBindable<ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>> software)
    {
        Software = software = software.GetBoundCopy();
        _Selected = new();

        var versions = TypedComboBox.Create(Array.Empty<PluginVersion>()).With(c => c.MinWidth = 100);
        var plugins = TypedComboBox.Create(Array.Empty<PluginType>()).With(c => c.MinWidth = 100);

        plugins.SelectionChanged += (obj, e) =>
        {
            versions.Items = Software.Value.GetValueOrDefault(plugins.SelectedItem)?.Keys.ToArray() ?? Array.Empty<PluginVersion>();
        };
        versions.SelectionChanged += (obj, e) => _Selected.Value = (plugins.SelectedItem, versions.SelectedItem);

        Software.SubscribeChanged(() => Dispatcher.UIThread.Post(() => plugins.Items = Software.Value.Keys.ToArray()), true);

        Children.Add(new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("* *"),
            RowDefinitions = RowDefinitions.Parse("Auto *"),
            Children =
            {
                new TextBlock().Bind("ui.plugin").WithRowColumn(0, 0),
                new TextBlock().Bind("ui.version").WithRowColumn(0, 1),
                plugins.WithRowColumn(1, 0),
                versions.WithRowColumn(1, 1),
            },
        });
    }
}
