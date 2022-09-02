using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using Node.Plugins.Deployment;

namespace Node.Registry;

public static class PowerShellInvoker
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public static IReadOnlyCollection<PSObject> InstallPlugin(PluginToDeploy plugin, string script, bool deleteInstaller)
    {
        string pldownload = null!;
        var invok = Invoke(script, setVars);

        if (deleteInstaller && Directory.Exists(pldownload))
            Directory.Delete(pldownload, true);

        return invok;


        void setVars(PowerShell psh)
        {
            var prox = psh.Runspace.SessionStateProxy;
            var plugintype = plugin.Type.ToString().ToLowerInvariant();
            var pluginver = plugin.Version;
            Path.GetInvalidFileNameChars().ToList().ForEach(x => pluginver = pluginver.Replace(x, '_'));

            prox.SetVariable("PLTORRENT", plugintype + "." + pluginver);

            prox.SetVariable("PLDOWNLOAD", pldownload = Path.Combine(prox.GetVariable("DOWNLOADS").ToString()!, plugintype, pluginver));
            prox.SetVariable("PLINSTALL", Path.Combine(prox.GetVariable("PLUGINS").ToString()!, plugintype, pluginver));
        }
    }

    public static IReadOnlyCollection<PSObject> Invoke(string script, Action<PowerShell>? runspaceModifFunc = null)
    {
        var runspace = RunspaceFactory.CreateRunspace();
        // TODO: restrictions on commands?


        runspace.Open();
        AddVariables(runspace);

        var psh = PowerShell.Create(runspace);
        runspaceModifFunc?.Invoke(psh);

        psh.AddStatement()
            .AddCommand("Import-Module")
            .AddParameter("Name", typeof(PowerShellInvoker).Assembly.Location);

        psh.AddStatement()
            .AddScript(script);

        var result = psh.Invoke(Array.Empty<object>(), new PSInvocationSettings() { ErrorActionPreference = ActionPreference.Stop });
        if (psh.InvocationStateInfo.Reason is not null)
            throw psh.InvocationStateInfo.Reason;

        foreach (var err in psh.Streams.Error)
            throw err.Exception;

        return result;
    }


    static void AddVariables(Runspace runspace)
    {
        var prox = runspace.SessionStateProxy;

        prox.SetVariable("PLUGINS", Path.GetFullPath("plugins"));

        prox.SetVariable("DOWNLOADS", Path.Combine(DownloadsDirectoryPath, "renderphin_plugins"));
        prox.SetVariable("LOCALAPPDATA", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        // TODO: other
    }


    static readonly string DownloadsDirectoryPath = GetDownloadDirectory();
    static string GetDownloadDirectory()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            return SHGetKnownFolderPath(Guid.Parse("374DE290-123F-4565-9164-39C4925E467B"), default);

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_DOWNLOAD_DIR");
            if (xdg is not null) return xdg;

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string downloads;
            if (Directory.Exists(downloads = Path.Combine(home, "Downloads"))) return downloads;
            if (Directory.Exists(downloads = Path.Combine(home, "Загрузки"))) return downloads;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);


        [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        static extern string SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = default);
    }
}
