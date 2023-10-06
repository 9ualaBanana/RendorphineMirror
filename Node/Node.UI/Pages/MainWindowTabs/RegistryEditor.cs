using System.Formats.Tar;
using Avalonia.Platform.Storage;

namespace Node.UI.Pages.MainWindowTabs;

public class RegistryEditor : Panel
{
    public required Apis Api { get; init; }
    public required ILogger<RegistryEditor> Logger { get; init; }
    readonly Bindable<ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>> Software = new();
    readonly Updaters.SoftwareUpdater SoftwareUpdater;

    public RegistryEditor(Updaters.SoftwareUpdater softwareUpdater, Bindable<ImmutableDictionary<PluginType, ImmutableDictionary<PluginVersion, SoftwareVersionInfo>>> software) : this(softwareUpdater) =>
        Software = software;
    public RegistryEditor(Updaters.SoftwareUpdater softwareUpdater)
    {
        SoftwareUpdater = softwareUpdater;
        Task.Run(async () => await SoftwareUpdater.Update(Software)).GetAwaiter().GetResult();

        async Task showerr(OperationResult result) =>
            await new OkMessageBox() { Text = result.Error.ToString() }.ShowDialog((Window) this.GetVisualRoot()!);
        FuncDispose disable()
        {
            IsEnabled = false;
            return new FuncDispose(() => IsEnabled = true);
        }

        var pluginselector = new PluginVersionSelector(Software);
        var plugininfo = new Panel();

        pluginselector.Selected.SubscribeChanged(() =>
        {
            plugininfo.Children.Clear();
            plugininfo.Children.Add(new TextBlock()
            {
                Text = $"""
                    Selected: {pluginselector.Selected.Value.type} {pluginselector.Selected.Value.version}
                    {JsonConvert.SerializeObject(Software.Value[pluginselector.Selected.Value.type][pluginselector.Selected.Value.version], Formatting.Indented)}
                    """,
            });
        });

        Children.Add(new ScrollViewer()
        {
            Content = new Grid()
            {
                RowDefinitions = RowDefinitions.Parse("Auto Auto Auto Auto"),
                Children =
                {
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 0, 0, 10),
                        Children =
                        {
                            new MPButton()
                            {
                                Text = "Reload list",
                                OnClickSelf = async self =>
                                {
                                    using var _ = disable();

                                    var result = await Reload();
                                    if (result) self.FlashOk().Consume();
                                    else await showerr(result);
                                },
                            },
                            new MPButton()
                            {
                                Text = "Upload",
                                OnClickSelf = async self =>
                                {
                                    using var _ = disable();

                                    var result = await Upload();
                                    if (result) self.FlashOk().Consume();
                                    else await showerr(result);
                                },
                            },
                        },
                    }.WithRow(0),
                    pluginselector.WithRow(1),
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new MPButton()
                            {
                                Text = "Download",
                                OnClickSelf = async self =>
                                {
                                    if (!pluginselector.IsSelected) return;
                                    using var _ = disable();

                                    var result = await Download(pluginselector.Selected.Value.type, pluginselector.Selected.Value.version);
                                    if (result) self.FlashOk().Consume();
                                    else await showerr(result);
                                },
                            },
                            new MPButton()
                            {
                                Text = "Delete",
                                Background = Brushes.Red,
                                OnClickSelf = async self =>
                                {
                                    if (!pluginselector.IsSelected) return;
                                    using var _ = disable();

                                    var result = await Delete(pluginselector.Selected.Value.type, pluginselector.Selected.Value.version);
                                    if (result) self.FlashOk().Consume();
                                    else await showerr(result);
                                },
                            },
                        },
                    }.WithRow(2),
                    plugininfo.WithRow(3),
                },
            },
        });
    }

    async Task<OperationResult> Reload() => await SoftwareUpdater.Update(Software);

    async Task<OperationResult> Delete(PluginType type, PluginVersion version) => await
        Api.Api.ApiGet($"{Apis.RegistryUrl}/admin/delete", "Deleting plugin", Api.AddSessionId(("type", type.ToString()), ("version", version.ToString()))).AsTask()
            .Next(Reload);

    async Task<OperationResult> Download(PluginType type, PluginVersion version)
    {
        var targets = await ((Window) this.GetVisualRoot().ThrowIfNull()).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Choose where to download",
            AllowMultiple = false,
        });
        if (targets.Count == 0) return true;

        var targetdir = targets[0].Path.AbsolutePath;

        using var _ = Directories.DisposeDelete(Path.Combine(targetdir, $"{type}_{version}.tar"), out var targetfile);
        var download = await Api.Api.ApiGetFile($"{Apis.RegistryUrl}/admin/download", targetfile, "Downloading plugin files", Api.AddSessionId(("type", type.ToString()), ("version", version.ToString())));
        if (!download) return download;

        using var stream = File.OpenRead(targetfile);
        await TarFile.ExtractToDirectoryAsync(stream, targetdir, true);

        return await Reload();
    }
    async Task<OperationResult> Upload()
    {
        var targets = await ((Window) this.GetVisualRoot().ThrowIfNull()).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Choose a dir to upload",
            AllowMultiple = false,
        });
        if (targets.Count == 0) return true;

        var target = targets[0].Path.AbsolutePath;

        using var ms = new MemoryStream();
        await TarFile.CreateFromDirectoryAsync(target, ms, false);
        ms.Position = 0;

        using var postcontent = new MultipartFormDataContent() { { new StreamContent(ms) { Headers = { ContentType = new("application/x-tar") } }, "file", "file.tar" }, };
        var upload = await Api.Api.ApiPost(global::Common.Api.AppendQuery($"{Apis.RegistryUrl}/admin/upload", Api.AddSessionId()), "Uploading plugin files", postcontent);
        if (!upload) return upload;

        return await Reload();
    }
}
