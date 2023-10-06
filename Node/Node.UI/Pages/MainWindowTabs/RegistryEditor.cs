using Avalonia.Platform.Storage;

namespace Node.UI.Pages.MainWindowTabs;

public class RegistryEditor : Panel
{
    readonly Apis Api;
    readonly ILogger<RegistryEditor> Logger;

    public RegistryEditor(Apis api, Updaters.SoftwareUpdater softwareUpdater, Bindable<ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>> software, ILogger<RegistryEditor> logger)
    {
        Api = api;
        Logger = logger;

        var pluginselector = new PluginVersionSelector(software);
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
                        var result = await softwareUpdater.Update(software);
                        await self.FlashErrorIfErr(result);
                    },
                }.WithRow(1),
            },
        });
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Download(PluginType.ImageDetector, "1.0.5").Consume();
    }

    async Task Download(PluginType type, PluginVersion version)
    {
        var targets = await ((Window) this.GetVisualRoot().ThrowIfNull()).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Choose where to download",
            AllowMultiple = false,
        });
        var target = targets[0].Path.AbsolutePath;

        using var response = await Api.Api.Client.GetAsync(global::Common.Api.AppendQuery($"{Apis.RegistryUrl}/admin/download", Api.AddSessionId(("type", type.ToString()), ("version", version.ToString())))).ConfigureAwait(false);
        await global::Common.Api.LogRequest(response, null, "Downloading plugin files", Logger, default);

        using var stream = await response.Content.ReadAsStreamAsync();
        using var targetstream = File.Create(Path.Combine(target, $"{type}_{version}.tar"));
        await stream.CopyToAsync(targetstream).ConfigureAwait(false);
    }
}
