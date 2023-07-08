namespace Node.Plugins.Discoverers;

internal class DotnetRuntimePluginDiscoverer : IPluginDiscoverer
{
    public async Task<IEnumerable<Plugin>> DiscoverAsync()
    {
        /*
            Host:
            Version:      7.0.8
            Architecture: x64
            Commit:       4b0550942d

            .NET SDKs installed:
            No SDKs were found.

            .NET runtimes installed:
            Microsoft.NETCore.App 7.0.8 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
            Microsoft.WindowsDesktop.App 7.0.8 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]

            Other architectures found:
            None

            Environment variables:
            Not set

            global.json file:
            Not found

            Learn more:
            https://aka.ms/dotnet/info

            Download .NET:
            https://aka.ms/dotnet/download
        */

        var dotnetexe = ProcessLauncher.FindInPath("dotnet");
        if (!dotnetexe) return Enumerable.Empty<Plugin>();

        var info = await new ProcessLauncher(dotnetexe.Value, "--info")
            .ExecuteFullAsync();

        var plugins = new List<Plugin>();
        var i = 0;
        while (true)
        {
            var idx1 = info.IndexOf("Microsoft.NETCore.App ", i, StringComparison.Ordinal);
            if (idx1 == -1) break;
            idx1 += "Microsoft.NETCore.App ".Length;

            var idx2 = info.IndexOf(" [", idx1 + 1, StringComparison.Ordinal);
            if (idx2 == -1) break;

            i = idx2;
            var version = info[idx1..idx2];
            plugins.Add(new Plugin(PluginType.DotnetRuntime, version, dotnetexe.Value));
        }

        return plugins;
    }
}
