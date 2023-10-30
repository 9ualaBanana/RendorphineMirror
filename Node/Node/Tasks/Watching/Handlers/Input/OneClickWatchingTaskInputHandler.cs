
using System.IO.Compression;

namespace Node.Tasks.Watching.Handlers.Input;

public class OneClickWatchingTaskInputHandler : WatchingTaskInputHandler<OneClickWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    public required PluginList PluginList { get; init; }

    public override void StartListening() => StartThreadRepeated(5_000, RunOnce);

    public async Task RunOnce()
    {
        Directory.CreateDirectory(Input.TestMzpDirectory);

        try
        {
            var betamzp = Directory.GetFiles(Input.TestMzpDirectory)
                .Where(p => Path.GetFileName(p).StartsWith("oneclick") && p.EndsWith(".mzp"))
                .Max();

            if (betamzp is not null)
            {
                var plugin = new Plugin(PluginType.OneClick, Path.GetFileNameWithoutExtension(betamzp)!.Substring("oneclickexport.v".Length), betamzp);
                await Run(plugin, Input.TestInputDirectory, Input.TestOutputDirectory, Input.TestLogDirectory);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        try
        {
            var plugin = PluginList.GetPlugin(PluginType.OneClick);
            await Run(plugin, Input.InputDirectory, Input.OutputDirectory, Input.LogDirectory);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    public async Task Run(Plugin mzp, string input, string output, string log)
    {
        var max = PluginList.GetPlugin(PluginType.Autodesk3dsMax);
        var unity = PluginList.GetPlugin(PluginType.Unity);

        Directory.CreateDirectory(output);
        Directory.CreateDirectory(input);
        Directory.CreateDirectory(log);

        var currentversion = Directory.GetFiles(output, "*.mzp")
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault()
            ?.Substring("oneclickexport.v".Length);

        if (currentversion != mzp.Version)
        {
            await Install(mzp, max);
            await CheckInstallation(mzp, max);
            await MoveOldVersion(mzp, output, currentversion);
        }

        foreach (var zip in Directory.GetFiles(input, "*.zip"))
            await ProcessArchive(zip, output, log, max, unity);
    }

    async Task ProcessArchive(string zip, string output, string log, Plugin max, Plugin unity)
    {
        var dir = Path.Combine(output, Path.GetFileNameWithoutExtension(zip));
        if (Directory.Exists(dir))
            return;

        using var _logscope = Logger.BeginScope($"Processing {Path.GetFileName(zip)}");

        Logger.Info($"Procesing {zip}");

        Logger.Info($"Extracting");
        ZipFile.ExtractToDirectory(zip, dir);
        Logger.Info("Extracted");
        var scenefile = Directory.GetFiles(dir, "*.max").Single();
        Logger.Info($"Scene file: {scenefile}");

        var unitydir = Path.Combine(dir, "unity", "Assets");
        Directory.CreateDirectory(unitydir);
        Logger.Info($"Target directory: {unitydir}");

        await runMax();
        await runUnity();

        Logger.Info("Success.");


        async Task runMax()
        {
            var launcher = new ProcessLauncher(max.Path)
            {
                Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
                ThrowOnStdErr = false,
                ThrowOnNonZeroExitCode = false,
                Arguments =
                {
                    // minimized, dialog boxes suppressed
                    "-ms", "-silent",

                    // log path
                    "-log", Directories.NumberedNameInDirectory(log, "log{0:0000}.log"),

                    // script parameters
                    /*
                    int - target engine; 1 = unreal, 2 = unity
                    string - output dir; should already exist
                    int - existing texture mode; 1 = skip copying, 2 = 256px, 3=512, 4=1024, 5=2048, 6=4096
                    int - bake texture mode; 1 = skip baking, 2 = 128px, 3=256, 4=512, 5=1024, 6=2048, 7=4096
                    bool int int - render cameras (true\false) and frame width height (always should be specified)
                    */
                    "-mxs", $"oneclickexport.oc000 2 @\"{unitydir}\" 3 3 true 960 540",

                    // scene to export
                    scenefile,
                },
            };

            Logger.Info("Launching 3dsmax");
            await launcher.ExecuteAsync();
            Logger.Info("Conversion completed");

            await ValidateConversionSuccessful(zip, output);
        }
        async Task runUnity()
        {
            var unityProjectDir = @"C:\UnityStore\OCHDRP22+";
            Directories.Copy(unitydir, Path.Combine(unityProjectDir, "Assets"));

            var launcher = new ProcessLauncher(unity.Path)
            {
                Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
                // ThrowOnStdErr = false,
                // ThrowOnNonZeroExitCode = false,
                Arguments =
                {
                    "-projectPath", unityProjectDir,
                    "-executeMethod", "OCBatchScript.StartBake",
                },
            };

            Logger.Info("Launching unity");
            await launcher.ExecuteAsync();
            Logger.Info("Unity render completed");
        }
    }

    async Task ValidateConversionSuccessful(string zip, string output)
    {
        Logger.Info("Validating conversion");

        var dir = Path.Combine(output, Path.GetFileNameWithoutExtension(zip), "unity", "Assets");
        if (!Directory.Exists(dir))
            throw new Exception("Result directory does not exists");

        var logfiles = Directory.GetDirectories(dir)
            .Select(dir => Path.Combine(dir, Path.GetFileName(dir) + ".txt"))
            .Where(File.Exists)
            .ToArray();

        if (logfiles.Length == 0)
            throw new Exception($"Log file was not found in {dir}");

        foreach (var logfile in logfiles)
        {
            var data = await File.ReadAllTextAsync(logfile);
            if (data.ContainsOrdinal("Export completed."))
            {
                Logger.Info("Conversion successful");
                return;
            }
        }

        throw new Exception("'Export completed.' was not found in the log");
    }

    async Task Install(Plugin mzp, Plugin max)
    {
        Logger.Info("Installing the plugin");

        foreach (var process in Process.GetProcessesByName("3dsmax"))
        {
            try { process.Kill(); }
            catch { }
        }

        var launcher = new ProcessLauncher(max.Path)
        {
            Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
            ThrowOnStdErr = false,
            ThrowOnNonZeroExitCode = false,
            Timeout = TimeSpan.FromMinutes(5),
            Arguments = { "-ms", "-silent", "-mxs", $"fileIn @\"{mzp.Path}\"" },
        };
        await launcher.ExecuteAsync();

        Logger.Info("Plugin installed");
    }


    async Task CheckInstallation(Plugin mzp, Plugin max)
    {
        Logger.Info("Checking plugin installation");

        using var reader = File.OpenRead(mzp.Path);
        var entry = new ZipArchive(reader).GetEntry("oneclickreadme.txt").ThrowIfNull("OneClick version was not found in mzp");
        using var entrystream = new StreamReader(entry.Open());
        var expectedversion = await entrystream.ReadToEndAsync();

        // %localAppData%\Autodesk\3dsMax\20?? - 64bit\ENU\scripts\startup\oneclickreadme.txt
        var installedpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Autodesk", "3dsMax", $"{max.Version} - 64bit", "ENU", "scripts", "startup", "oneclickreadme.txt");

        var installedversion = File.ReadAllText(installedpath);

        if (installedversion != expectedversion)
            throw new Exception($"Invalid mzp installation: versions are not equal ({installedversion} vs {expectedversion})");

        Logger.Info($"Installed plugin version: {installedversion}");
    }

    async Task MoveOldVersion(Plugin mzp, string output, string? currentversion)
    {
        Logger.Info("Moving old dirs");

        if (Directory.Exists(output))
            Directory.Move(output, output + (currentversion ?? "0.0"));

        Directory.CreateDirectory(output);
        var target = Path.Combine(output, Path.GetFileName(mzp.Path));
        File.Copy(mzp.Path, target);

        Logger.Info($"Old output dir moved to {target}");
    }
}
