using System.IO.Compression;

namespace Node.Tasks.Watching.Handlers.Input;

public class OneClickWatchingTaskInputHandlerRunner
{
    /// <summary> C:\oneclick\input\{SmallGallery.zip} </summary>
    public required string ZipFilePath { get; init; }

    /// <summary> C:\oneclick\input </summary>
    public required string InputDir { get; init; }

    /// <summary> C:\oneclick\output </summary>
    public required string OutputDir { get; init; }

    /// <summary> C:\oneclick\log </summary>
    public required string LogDir { get; init; }

    /// <summary> C:\OneClickUnityDefaultProjects\ </summary>
    public required string UnityTemplatesDir { get; init; }

    public required string UnityTemplatesGitCommitHash { get; init; }

    public required IPluginList PluginList { get; init; }
    public required Plugin TdsMaxPlugin { get; init; }
    public required Plugin OneClickPlugin { get; init; }

    ProjectExportInfo? _ExportInfo;
    ProjectExportInfo ExportInfo
    {
        get
        {
            if (_ExportInfo is null)
            {
                if (!File.Exists(ExportInfoFile))
                    File.WriteAllText(ExportInfoFile, "{}");

                _ExportInfo = JsonConvert.DeserializeObject<ProjectExportInfo>(File.ReadAllText(ExportInfoFile)).ThrowIfNull();
            }

            return _ExportInfo;
        }
    }

    public required ILogger Logger { get; init; }

    /// <summary> C:\oneclick\output\{SmallGallery} </summary>
    string NamedOutputDirectory => Directories.DirCreated(OutputDir, Path.GetFileNameWithoutExtension(ZipFilePath));

    /// <summary> C:\oneclick\output\{SmallGallery}\unity </summary>
    string UnityResultDirectory => Directories.DirCreated(NamedOutputDirectory, "unity");

    /// <summary> C:\oneclick\output\{SmallGallery}\unity\Assets </summary>
    string UnityAssetsResultDirectory => Directories.DirCreated(UnityResultDirectory, "Assets");

    /// <summary> C:\oneclick\output\{SmallGallery}\unity\Assets\{Handling_Machining_v6} </summary>
    string UnityAssetsSceneResultDirectory
    {
        get
        {
            var path = Directory.GetFiles(UnityAssetsResultDirectory)
                .SingleOrDefault(file => Path.GetExtension(file) == ".fbx");

            if (path is not null)
                path = Path.ChangeExtension(path, null);
            else
                path = Directory.GetDirectories(UnityAssetsResultDirectory)
                    .SingleOrDefault(dir => Path.GetFileName(dir) != "OneClickImport");

            return path.ThrowIfNull();
        }
    }

    /// <summary> {Handling_Machining_v6} </summary>
    string UnitySceneName => Path.GetFileName(UnityAssetsSceneResultDirectory);

    /// <summary> C:\oneclick\output\{SmallGallery}\unity\Assets\{Handling_Machining_v6}\renders </summary>
    string UnityRendersDirectory => Directories.DirCreated(UnityAssetsSceneResultDirectory, "renders");

    /// <summary> C:\oneclick\output\{SmallGallery}\exportinfo.txt </summary>
    string ExportInfoFile => Path.GetFullPath(Path.Combine(NamedOutputDirectory, "exportinfo.txt"));


    public static async Task RunAll(string inputdir, string outputdir, string logdir, IPluginList plugins, ILogger logger, Plugin? oneclick)
    {
        Directory.CreateDirectory(outputdir);
        Directory.CreateDirectory(inputdir);
        Directory.CreateDirectory(logdir);

        var max = plugins.GetPlugin(PluginType.Autodesk3dsMax);
        oneclick ??= plugins.GetPlugin(PluginType.OneClick);

        var unityTemplatesDir = @"C:\\OneClickUnityDefaultProjects";

        await InstallOneClickIfNeeded(outputdir, oneclick, max, logger);
        var unityTemplatesCommitHash = await UpdateUnityTemplates(unityTemplatesDir, logger);

        foreach (var zipfilepath in Directory.GetFiles(inputdir, "*.zip"))
        {
            var runner = new OneClickWatchingTaskInputHandlerRunner()
            {
                ZipFilePath = zipfilepath,
                InputDir = inputdir,
                OutputDir = outputdir,
                LogDir = logdir,
                UnityTemplatesDir = unityTemplatesDir,
                UnityTemplatesGitCommitHash = unityTemplatesCommitHash,
                PluginList = plugins,
                TdsMaxPlugin = max,
                OneClickPlugin = oneclick,
                Logger = logger,
            };

            await runner.Run();
        }
    }

    static async Task InstallOneClickIfNeeded(string outputdir, Plugin oneclick, Plugin max, ILogger logger)
    {
        var currentOneClickVersion = Directory.GetFiles(outputdir, "*.mzp")
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault()
            ?.Substring("oneclickexport.v".Length);

        if (currentOneClickVersion == oneclick.Version)
            return;

        await InstallOneClick(oneclick, max, logger);
        await CheckOneClickInstallation(oneclick, max, logger);
        await MoveOneClickOldVersion(oneclick, outputdir, currentOneClickVersion, logger);
    }
    static async Task<string> UpdateUnityTemplates(string unityTemplatesDir, ILogger logger)
    {
        try
        {
            await startGit("pull");
        }
        catch (NodeProcessException)
        {
            await startGit("reset", "--hard");
            await startGit("pull");
        }

        return (await startGit("rev-parse", "--verify", "HEAD")).Trim();



        async Task<string> startGit(params string[] args)
        {
            return await new ProcessLauncher("git", args)
            {
                Logging = { ILogger = logger, LogStartMessage = false, StdErr = Microsoft.Extensions.Logging.LogLevel.Trace, StdOut = Microsoft.Extensions.Logging.LogLevel.Trace },
                ThrowOnStdErr = false,
                WorkingDirectory = unityTemplatesDir,
            }.ExecuteFullAsync();
        }
    }

    async Task SaveExportInfo() => await File.WriteAllTextAsync(ExportInfoFile, JsonConvert.SerializeObject(ExportInfo));
    async Task ReportError(Exception exception)
    {
        Logger.Error(exception);

        try
        {
            var scenename = "<none>";
            try { scenename = UnitySceneName; }
            catch { }

            var message = exception.Message;
            var errorstr = $"""
                Error from renderfin OneClick export:
                Scene name: {scenename}
                Archive name: {Path.GetFileNameWithoutExtension(ZipFilePath)}

                ```
                {message.Replace(@"\", @"\\")}
                ```
                """;

            var query = Api.ToQuery(("error", errorstr));
            using var result = await Api.Default.Client.PostAsync($"{Settings.ServerUrl}/oneclick/display_render_error?{query}", content: null);
            result.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending the error to tg bot: {ex}");
        }
    }

    async Task ReportResult()
    {
        using var content = new MultipartFormDataContent();
        foreach (var renderfile in Directory.GetFiles(UnityRendersDirectory, "*.png"))
            content.Add(new StreamContent(File.OpenRead(renderfile)), "renders", Path.GetFileName(renderfile));

        using var result = await Api.Default.Client.PostAsync($"{Settings.ServerUrl}/oneclick/display_renders", content);
        result.EnsureSuccessStatusCode();
    }


    async Task Run()
    {
        using var _logscope = Logger.BeginScope(Path.GetFileName(ZipFilePath));

        try
        {
            try { await RunMax(); }
            catch (Exception ex)
            {
                ExportInfo.OneClick = new(OneClickPlugin.Version.ToString(), false);
                throw new Exception($"[3dsmax] [{Path.GetFileNameWithoutExtension(ZipFilePath)}] {ex.Message}", ex);
            }

            await SaveExportInfo();

            try { await RunUnity(); }
            catch (Exception ex) { throw new Exception($"[Unity] [{Path.GetFileNameWithoutExtension(ZipFilePath)}] {ex.Message}", ex); }
        }
        catch (Exception ex)
        {
            try { await ReportError(ex); }
            catch { }
        }
        finally
        {
            await SaveExportInfo();
        }
    }
    async Task RunMax()
    {
        if (ExportInfo.OneClick?.Version == OneClickPlugin.Version)
            return;

        using var _ = Logger.BeginScope($"3dsmax");

        Logger.Info($"Extracting");
        Directory.Delete(NamedOutputDirectory, true);
        ZipFile.ExtractToDirectory(ZipFilePath, NamedOutputDirectory);
        Logger.Info("Extracted");

        var scenefile = Directory.GetFiles(NamedOutputDirectory, "*.max", SearchOption.AllDirectories)
            .Where(zip => !zip.ContainsOrdinal("backup"))
            .MaxBy(File.GetLastWriteTimeUtc);
        scenefile ??= Directory.GetFiles(NamedOutputDirectory, "*.max", SearchOption.AllDirectories)
            .MaxBy(File.GetLastWriteTimeUtc);
        scenefile.ThrowIfNull("No .max file found");

        Logger.Info($"Scene file: {scenefile}; Target directory: {UnityAssetsResultDirectory}");


        var launcher = new ProcessLauncher(TdsMaxPlugin.Path)
        {
            Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
            ThrowOnStdErr = false,
            ThrowOnNonZeroExitCode = false,
            Arguments =
            {
                // minimized, dialog boxes suppressed
                "-ms", "-silent",

                // log path
                "-log", Directories.NumberedNameInDirectory(LogDir, "log{0:0000}.log"),

                // script parameters
                /*
                int - target engine; 1 = unreal, 2 = unity
                string - output dir; should already exist
                int - existing texture mode; 1 = skip copying, 2 = 256px, 3=512, 4=1024, 5=2048, 6=4096
                int - bake texture mode; 1 = skip baking, 2 = 128px, 3=256, 4=512, 5=1024, 6=2048, 7=4096
                bool int int - render cameras (true\false) and frame width height (always should be specified)
                (1.35+) bool - deploy importer from internal zip
                */
                "-mxs", $"oneclickexport.oc000 2 @\"{UnityAssetsResultDirectory}\" 3 3 true 960 540 false",

                // scene to export
                scenefile.Replace('\\', '/'),
            },
        };

        Logger.Info("Launching 3dsmax");
        await launcher.ExecuteAsync();
        Logger.Info("Conversion completed");

        Logger.Info("Validating");
        await validateConversionSuccessful();
        Logger.Info("Success.");
        ExportInfo.OneClick = new(OneClickPlugin.Version.ToString(), true);


        async Task validateConversionSuccessful()
        {
            Logger.Info("Validating conversion");

            var dir = UnityAssetsResultDirectory;
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
    }
    async Task RunUnity()
    {
        if (ExportInfo.OneClick?.Successful != true)
            return;

        using var _ = Logger.BeginScope($"Unity");
        ExportInfo.Unity ??= new();

        var unityLogDir = Directories.DirCreated(LogDir, "unity");

        var scenePath = UnityAssetsSceneResultDirectory;
        var sceneJustName = UnitySceneName;
        var unityTemplateNames = new[] { "OCURP21+", /*"OCHDRP22+"*/ };
        foreach (var unityTemplateName in unityTemplateNames)
            await tryProcess(unityTemplateName);


        bool readImporterVersionFile(string path, [MaybeNullWhen(false)] out UnityBakedExportInfo importerVersion)
        {
            importerVersion = null;
            if (!File.Exists(path))
                return false;

            var infofilecontents = File.ReadAllText(path);
            var infofile = infofilecontents.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (infofile.Length != 3)
            {
                Logger.Error($"Invalid {Path.GetFileName(path)}: \n" + infofilecontents);
                return false;
            }

            importerVersion = new(infofile[0], infofile[1], infofile[2]);
            return true;
        }
        async Task tryProcess(string unityTemplateName)
        {
            var ocImporterVersion = null as UnityBakedExportInfo;

            try
            {
                // C:\OneClickUnityDefaultProjects\{OCURP21+}
                var unityTemplateDir = Path.Combine(UnityTemplatesDir, unityTemplateName);

                // C:\OneClickUnityDefaultProjects\{OCURP21+}\Assets
                var unityTemplateAssetsDir = Path.Combine(unityTemplateDir, "Assets");

                // C:\OneClickUnityDefaultProjects\{OCURP21+}\Assets\OCImporterVersion.txt
                var ocImporterVersionFile = Path.Combine(unityTemplateAssetsDir, "OCImporterVersion.txt");

                if (!readImporterVersionFile(ocImporterVersionFile, out ocImporterVersion))
                    return;

                if (!needsConversion())
                    return;

                await process(unityTemplateName, unityTemplateDir, unityTemplateAssetsDir, ocImporterVersion);
            }
            catch (Exception ex)
            {
                if (ocImporterVersion is not null)
                    ExportInfo.Unity[unityTemplateName] = new(ocImporterVersion.ImporterVersion, ocImporterVersion.UnityVersion, ocImporterVersion.RendererType, UnityTemplatesGitCommitHash, false);

                throw new Exception($"Could not process {unityTemplateName} for {sceneJustName}: {ex.Message}", ex);
            }


            bool needsConversion()
            {
                if (ExportInfo.Unity?.TryGetValue(unityTemplateName, out var info) != true || info is null)
                    return true;

                // convert only if either:
                // - versions are different
                // - versions are the same but commit hashes are different and the previous convertion was not successful

                if (info.ImporterVersion != ocImporterVersion.ImporterVersion)
                    return true;

                if (info.ImporterCommitHash != UnityTemplatesGitCommitHash && !info.Successful)
                    return true;

                return false;
            }
        }
        async Task process(string unityTemplateName, string unityTemplateDir, string unityTemplateAssetsDir, UnityBakedExportInfo ocImporterVersion)
        {
            var (importerVersion, unityVersion, rendererType) = ocImporterVersion;


            Logger.Info($"Importing {sceneJustName} with Unity {unityVersion} {rendererType} and importer v{importerVersion}");
            var unity = PluginList.GetPlugin(PluginType.Unity, unityVersion);

            foreach (var fbx in Directory.GetFiles(unityTemplateAssetsDir, "*.fbx"))
            {
                Logger.Info($"Deleting already completed file {fbx}");
                File.Delete(fbx);

                var dirname = Path.ChangeExtension(fbx, null);
                if (Directory.Exists(dirname))
                {
                    Logger.Info($"Deleting already completed dir {dirname}");
                    Directory.Delete(dirname, true);
                }
            }

            var movedentries = Directory.GetFiles(UnityAssetsResultDirectory)
                .Concat(Directory.GetDirectories(UnityAssetsResultDirectory))
                .Select(Path.GetFileName)
                .Where(f => f != "OneClickImport")
                .WhereNotNull()
                .ToArray();

            try
            {
                Logger.Info($"Source entries: {string.Join(", ", movedentries)}");
                Logger.Info($"Merging {UnityAssetsResultDirectory} with  {unityTemplateAssetsDir}");
                Directories.Merge(UnityAssetsResultDirectory, unityTemplateAssetsDir);

                await killUnity();
                async Task killUnity()
                {
                    Logger.Info("Killing unity");
                    await new ProcessLauncher("taskkill", "/IM", "Unity.exe", "/F") { ThrowOnStdErr = false, ThrowOnNonZeroExitCode = false }
                        .ExecuteAsync();
                }

                var bakeCompletedFile = Path.Combine(unityTemplateAssetsDir, "BakeCompleted.txt");
                if (File.Exists(bakeCompletedFile))
                    File.Delete(bakeCompletedFile);

                Logger.Info("Launching unity");

                var logFileName = sceneJustName + "_log.log";
                foreach (var invalid in Path.GetInvalidPathChars())
                    logFileName = logFileName.Replace(invalid, '_');

                //NonAdminRunner.RunAsDesktopUserWaitForExit(unity.Path, );
                var launcher = new ProcessLauncher(unity.Path)
                {
                    Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
                    ThrowOnStdErr = false,
                    ThrowOnNonZeroExitCode = false,
                    Timeout = TimeSpan.FromMinutes(10),
                    Arguments =
                    {
                        "-accept-apiupdate",
                        "-batchmode",
                        "-projectPath", unityTemplateDir,
                        "-executeMethod", "OCBatchScript.StartBake",
                        "-noLM",
                        "-logFile", Path.Combine(unityLogDir, logFileName),
                    },
                };
                await launcher.ExecuteAsync();

                for (int i = 0; i < 60; i++)
                {
                    if (File.Exists(bakeCompletedFile))
                        break;

                    await System.Threading.Tasks.Task.Delay(1000);
                }

                if (!File.Exists(bakeCompletedFile))
                    throw new Exception($"{bakeCompletedFile} does not exists");

                if (!readImporterVersionFile(bakeCompletedFile, out var newBakedInfo))
                    throw new Exception($"{bakeCompletedFile} is not parsable; Contents: \n```\n{bakeCompletedFile}\n```");

                ExportInfo.Unity[unityTemplateName] = new(newBakedInfo.ImporterVersion, newBakedInfo.UnityVersion, newBakedInfo.RendererType, UnityTemplatesGitCommitHash, true);
                moveBack();

                var buildProjectDir = Path.Combine(unityTemplateDir, "Builds");
                var unityImportResultDir = UnityAssetsSceneResultDirectory;

                // entrance_hall_for_export_[2021.3.32f1]_[URP]_[50]
                var buildResultDir = Path.Combine(unityImportResultDir, "Builds", $"{sceneJustName}_[{unityVersion}]_[{rendererType}]_[{importerVersion}]");
                if (!Directory.Exists(buildResultDir))
                {
                    Logger.Error($"{buildResultDir} was not found; searching for an empty dir");
                    buildResultDir = Directory.GetDirectories(Path.Combine(unityImportResultDir, "Builds"))
                        .Where(dir => Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                        .Single();
                }


                if (Directory.Exists(buildResultDir))
                    Directory.Delete(buildResultDir, true);

                {
                    var moved = false;
                    for (int i = 0; i < 60 * 60; i++)
                    {
                        var exeprocess = Process.GetProcesses().Where(proc =>
                        {
                            try { return Path.GetFileName(proc.MainModule?.FileName)?.StartsWith(sceneJustName) == true; }
                            catch { return false; }
                        }).FirstOrDefault();

                        if (exeprocess is not null)
                        {
                            if (i == 60 * 60 - 1)
                                exeprocess.Kill();
                            else
                            {
                                Logger.Info($"Found the exported app exe running: {exeprocess.Id} {exeprocess.MainModule!.FileName}; waiting a sec");
                                await System.Threading.Tasks.Task.Delay(5000);
                                continue;
                            }
                        }

                        try
                        {
                            moveBuildResult();
                            moved = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.Info($"Could not move the build dir {buildProjectDir} to {buildResultDir}: {ex.Message}; retrying after a sec");
                            await System.Threading.Tasks.Task.Delay(5000);
                        }
                    }

                    if (!moved)
                    {
                        await killUnity();
                        moveBuildResult();
                    }


                    void moveBuildResult()
                    {
                        Logger.Info($"Moving the build dir from {buildProjectDir} to {buildResultDir}");
                        if (Directory.Exists(buildResultDir))
                            Directory.Delete(buildResultDir, true);

                        Directory.Move(buildProjectDir, buildResultDir);
                    }
                }
            }
            finally
            {
                moveBack();
            }

            await ReportResult();
            Logger.Info("Completed");


            void moveBack()
            {
                if (movedentries.Length == 0)
                    return;

                if (!Directory.Exists(UnityAssetsResultDirectory))
                    Directory.CreateDirectory(UnityAssetsResultDirectory);

                foreach (var entry in movedentries)
                {
                    var source = Path.Combine(unityTemplateAssetsDir, entry);
                    var destination = Path.Combine(UnityAssetsResultDirectory, entry);
                    Logger.Info($"Moving back {source} to {destination}");

                    if (Directory.Exists(source))
                        Directory.Move(source, destination);
                    else File.Move(source, destination);
                }

                movedentries = Array.Empty<string>();
            }
        }
    }


    static async Task InstallOneClick(Plugin mzp, Plugin max, ILogger logger)
    {
        logger.Info("Installing the plugin");

        // fix for vray not being silent enough
        const string vraySilentFix = "if setVRaySilentMode != undefined then setVRaySilentMode()";
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(max.Path).ThrowIfNull(), "scripts", "Startup", "oneclicksilent.ms"), vraySilentFix);

        foreach (var process in Process.GetProcessesByName("3dsmax"))
        {
            try { process.Kill(); }
            catch { }
        }

        var launcher = new ProcessLauncher(max.Path)
        {
            Logging = new ProcessLauncher.ProcessLogging() { ILogger = logger, },
            ThrowOnStdErr = false,
            ThrowOnNonZeroExitCode = false,
            Timeout = TimeSpan.FromMinutes(5),
            Arguments = { "-ms", "-silent", "-mxs", $"fileIn @\"{mzp.Path}\"" },
        };
        await launcher.ExecuteAsync();

        logger.Info("Plugin installed");
    }
    static async Task CheckOneClickInstallation(Plugin mzp, Plugin max, ILogger logger)
    {
        logger.Info("Checking plugin installation");

        using var reader = File.OpenRead(mzp.Path);
        var entry = new ZipArchive(reader).GetEntry("oneclickreadme.txt").ThrowIfNull("OneClick version was not found in mzp");
        using var entrystream = new StreamReader(entry.Open());
        var expectedversion = await entrystream.ReadToEndAsync();

        // %localAppData%\Autodesk\3dsMax\20?? - 64bit\ENU\scripts\startup\oneclickreadme.txt
        var installedpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Autodesk", "3dsMax", $"{max.Version} - 64bit", "ENU", "scripts", "startup", "oneclickreadme.txt");

        var installedversion = File.ReadAllText(installedpath);

        if (installedversion != expectedversion)
            throw new Exception($"Invalid mzp installation: versions are not equal (installed {installedversion} vs expected {expectedversion})");

        logger.Info($"Installed plugin version: {installedversion}");
    }
    static async Task MoveOneClickOldVersion(Plugin mzp, string output, string? currentversion, ILogger logger)
    {
        logger.Info("Moving old dirs");

        if (Directory.Exists(output))
        {
            if (currentversion is null) Directory.Delete(output, true);
            else Directory.Move(output, output + currentversion);
        }

        Directory.CreateDirectory(output);
        var target = Path.Combine(output, Path.GetFileName(mzp.Path));
        File.Copy(mzp.Path, target);

        logger.Info($"Old output dir moved to {target}");
    }


    public record UnityBakedExportInfo(string ImporterVersion, string UnityVersion, string RendererType);

    public record OneClickProjectExportInfo(string Version, bool Successful);
    public record UnityProjectExportInfo(string ImporterVersion, string UnityVersion, string RendererType, string ImporterCommitHash, bool Successful);
    public class ProjectExportInfo
    {
        public OneClickProjectExportInfo? OneClick { get; set; }
        public Dictionary<string, UnityProjectExportInfo>? Unity { get; set; }
    }
}
