
using System.IO.Compression;

namespace Node.Tasks.Watching.Handlers.Input;

public class OneClickWatchingTaskInputHandler : WatchingTaskInputHandler<OneClickWatchingTaskInputInfo>, ITypedTaskWatchingInput
{
    public static WatchingTaskInputType Type => WatchingTaskInputType.OneClick;

    public required PluginList PluginList { get; init; }

    public override void StartListening() => StartThreadRepeated(5_000, RunOnce);

    public async Task RunOnce()
    {
        var mzp = PluginList.TryGetPlugin(PluginType.OneClick).ThrowIfNull("No oneclick installed");
        var max = PluginList.TryGetPlugin(PluginType.Autodesk3dsMax).ThrowIfNull("No 3dsmax installed");

        Directory.CreateDirectory(Input.OutputDirectory);
        Directory.CreateDirectory(Input.InputDirectory);
        Directory.CreateDirectory(Input.LogDirectory);

        var currentversion = Directory.GetFiles(Input.OutputDirectory, "*.mzp")
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault()
            ?.Substring("oneclickexport.v".Length);

        if (currentversion != mzp.Version)
        {
            await install();
            await checkinstallation();
            await moveoldversion();
        }

        await processArchives();



        async Task processArchives()
        {
            foreach (var zip in Directory.GetFiles(Input.InputDirectory, "*.zip"))
                await processArchive(zip);
        }

        async Task processArchive(string zip)
        {
            var dir = Path.Combine(Input.OutputDirectory, Path.GetFileNameWithoutExtension(zip));
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
                    "-log", Directories.NumberedNameInDirectory(Input.LogDirectory, "log{0:0000}.log"),

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

            await validateConversionSuccessful(zip);
            Logger.Info("Success.");
        }

        async Task validateConversionSuccessful(string zip)
        {
            Logger.Info("Validating conversion");

            var dir = Path.Combine(Input.OutputDirectory, Path.GetFileNameWithoutExtension(zip), "unity", "Assets");
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
                    return;
            }

            throw new Exception("'Export completed.' was not found in the log");
        }


        async Task install()
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

        async Task checkinstallation()
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

        async Task moveoldversion()
        {
            Logger.Info("Moving old dirs");

            if (Directory.Exists(Input.OutputDirectory))
                Directory.Move(Input.OutputDirectory, Input.OutputDirectory + (currentversion ?? "0.0"));

            Directory.CreateDirectory(Input.OutputDirectory);
            var target = Path.Combine(Input.OutputDirectory, Path.GetFileName(mzp.Path));
            File.Copy(mzp.Path, target);

            Logger.Info($"Old output dir moved to {target}");
        }
    }
}
