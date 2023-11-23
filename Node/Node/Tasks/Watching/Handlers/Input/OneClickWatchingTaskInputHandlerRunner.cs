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

    /// <summary> C:\oneclick\result </summary>
    public required string ResultDir { get; init; }

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
            try
            {
                return _ExportInfo ??= JsonConvert.DeserializeObject<ProjectExportInfo>(File.ReadAllText(ExportInfoFile)).ThrowIfNull();
            }
            catch
            {
                return _ExportInfo = new();
            }
        }
    }

    public required ILogger Logger { get; init; }

    IReadOnlyList<OneClickWatchingTaskInputHandlerRunner> Runners { get; set; } = null!;

    string? _ProductName;
    /// <summary> {Handling_Machining_v6} </summary>
    /// <remarks> Will throw an exception if called before extracting the source zip </remarks>
    string ProductName { get => _ProductName ??= Path.GetFileNameWithoutExtension(GetMaxSceneFile(NamedOutputDirectory)); set => _ProductName = value; }

    /// <summary> C:\oneclick\output\{SmallGallery} </summary>
    string NamedOutputDirectory => Directories.DirCreated(OutputDir, Path.GetFileNameWithoutExtension(ZipFilePath));

    /// <summary> C:\oneclick\output\{SmallGallery}\unity </summary>
    string UnityResultDirectory => Directories.DirCreated(NamedOutputDirectory, "unity");

    /// <summary> C:\oneclick\output\{SmallGallery}\unity\Assets </summary>
    string UnityAssetsResultDirectory => Directories.DirCreated(UnityResultDirectory, "Assets");

    /// <summary> C:\oneclick\output\{SmallGallery}\unity\Assets\{Handling_Machining_v6} </summary>
    string UnityAssetsSceneResultDirectory => Path.Combine(UnityAssetsResultDirectory, ProductName);

    /// <summary> C:\oneclick\output\{SmallGallery}\exportinfo.txt </summary>
    string ExportInfoFile => Path.GetFullPath(Path.Combine(NamedOutputDirectory, "exportinfo.txt"));

    /// <summary> C:\oneclick\log\unity\{Handling_Machining_v6}_log.log </summary>
    string UnityLogFile
    {
        get
        {
            var logFileName = Path.GetFileNameWithoutExtension(ProductName) + "_log.log";
            foreach (var invalid in Path.GetInvalidPathChars())
                logFileName = logFileName.Replace(invalid, '_');

            return Path.Combine(Directories.DirCreated(LogDir, "unity"), logFileName);
        }
    }


    public static async Task RunAll(string inputdir, string outputdir, string resultdir, string logdir, IPluginList plugins, ILogger logger, Plugin? oneclick)
    {
        Directory.CreateDirectory(outputdir);
        Directory.CreateDirectory(inputdir);
        Directory.CreateDirectory(resultdir);
        Directory.CreateDirectory(logdir);

        var max = plugins.GetPlugin(PluginType.Autodesk3dsMax);
        oneclick ??= plugins.GetPlugin(PluginType.OneClick);

        var unityTemplatesDir = @"C:\\OneClickUnityDefaultProjects";

        await InstallOneClickIfNeeded(outputdir, oneclick, max, logger);
        var unityTemplatesCommitHash = await UpdateUnityTemplates(unityTemplatesDir, logger);

        var runners = Directory.GetFiles(inputdir, "*.zip")
            .Order()
            .Select(zipfilepath => new OneClickWatchingTaskInputHandlerRunner()
            {
                ZipFilePath = zipfilepath,
                InputDir = inputdir,
                OutputDir = outputdir,
                ResultDir = resultdir,
                LogDir = logdir,
                UnityTemplatesDir = unityTemplatesDir,
                UnityTemplatesGitCommitHash = unityTemplatesCommitHash,
                PluginList = plugins,
                TdsMaxPlugin = max,
                OneClickPlugin = oneclick,
                Logger = logger,
            })
            .ToArray();

        foreach (var runner in runners)
        {
            runner.Runners = runners;
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
            await startGit("add", ".");
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

    string GetExportInfoForLog()
    {
        var all = Runners.Count;
        var oneclickcompleted = Runners.Count(r => r.ExportInfo.OneClick is { Successful: true });
        var unityfullcompleted = Runners.Count(r => r.ExportInfo.Unity?.All(u => u.Value.Successful) == true);

        return $"""
            [ {all} input achives; {oneclickcompleted} 3dsmax completed; {unityfullcompleted} unity completed ]
            """;
    }

    async Task SaveExportInfo() => await File.WriteAllTextAsync(ExportInfoFile, JsonConvert.SerializeObject(ExportInfo));
    async Task ReportError(Exception exception)
    {
        Logger.Error(exception);

        var unityLogFile = UnityLogFile;
        var secondlog = null as string;
        try { secondlog = Path.Combine(UnityAssetsSceneResultDirectory, ProductName + ".log"); }
        catch { }

        var files = new List<string>();
        try
        {
            if (File.Exists(unityLogFile))
            {
                for (int i = 0; i < 10 || new FileInfo(unityLogFile).Length != 0; i++)
                {
                    await Task.Delay(1000);
                    continue;
                }
            }

            File.OpenRead(unityLogFile).Dispose();
            files.Add(unityLogFile);
        }
        catch { }

        if (secondlog is not null)
            try
            {
                File.OpenRead(secondlog).Dispose();
                files.Add(secondlog);
            }
            catch { }

        await ReportMessageToTg(exception.Message, files);
    }
    async Task ReportMessageToTg(string msg, IEnumerable<string> files)
    {
        try
        {
            var sceneinfo = "\nScene name: ";
            try { sceneinfo += ProductName; }
            catch { sceneinfo += "<none>"; }

            var content = new MultipartFormDataContent();
            try
            {
                foreach (var file in files)
                {
                    Logger.Info("Adding log file to requets " + file);
                    try { content.Add(new StreamContent(File.OpenRead(file)), "logs", Path.GetFileName(file)); }
                    catch { }
                }

                msg = $"""
                    Archive name: {Path.GetFileNameWithoutExtension(ZipFilePath)}{sceneinfo}
                    ```
                    {msg.Replace(@"\", @"\\")}
                    ```

                    {GetExportInfoForLog()}
                    """;

                Logger.Info("Sending the " + msg + " of " + string.Join(", ", content.Select(x => x.ToString())));

                if (!content.Any()) content = null;

                var query = Api.ToQuery(("error", msg));
                await Api.Default.ApiPost($"{Settings.ServerUrl}/oneclick/display_render_error?{query}", "Displaying render error", content);
            }
            finally
            {
                content?.Dispose();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending a message to tg bot: {ex}");
        }
    }

    async Task ReportResult(string sceneName)
    {
        using var content = new MultipartFormDataContent();
        foreach (var renderfile in Directory.GetFiles(Path.Combine(ResultDir, sceneName, "renders"), "*.mp4"))
            content.Add(new StreamContent(File.OpenRead(renderfile)), "renders", Path.GetFileName(renderfile));
        foreach (var renderfile in Directory.GetFiles(Path.Combine(ResultDir, sceneName, "renders"), "*.png"))
            content.Add(new StreamContent(File.OpenRead(renderfile)), "renders", Path.GetFileName(renderfile));

        var caption = $"{sceneName} from {Settings.NodeName}\n{GetExportInfoForLog()}";
        var query = Api.ToQuery(("caption", caption));
        await Api.Default.ApiPost($"{Settings.ServerUrl}/oneclick/display_renders?{query}", "Displaying renders", content);
    }

    static string GetMaxSceneFile(string dir)
    {
        var maxSceneFile = Directory.GetFiles(dir, "*.max", SearchOption.AllDirectories)
              .Where(zip => !zip.ContainsOrdinal("backup"))
              .MaxBy(File.GetLastWriteTimeUtc);
        maxSceneFile ??= Directory.GetFiles(dir, "*.max", SearchOption.AllDirectories)
            .MaxBy(File.GetLastWriteTimeUtc);

        return maxSceneFile.ThrowIfNull("No .max file found");
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
            try { Task.Run(async () => await ReportError(ex)).Consume(); }
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

        {
            var archiveOutputDirectory = NamedOutputDirectory;
            if (Directory.Exists(archiveOutputDirectory))
                Directory.Delete(archiveOutputDirectory, true);

            Logger.Info($"Extracting {ZipFilePath} to {archiveOutputDirectory}");
            ZipFile.ExtractToDirectory(ZipFilePath, archiveOutputDirectory);

            // extract all archives inside, recursively
            while (true)
            {
                var zips = Directory.GetFiles(archiveOutputDirectory, "*.zip", SearchOption.AllDirectories);
                if (zips.Length == 0) break;

                foreach (var zip in zips)
                {
                    var dest = Path.GetDirectoryName(zip)!;

                    Logger.Info($"Extracting {zip} to {dest}");
                    ZipFile.ExtractToDirectory(zip, dest);
                    File.Delete(zip);
                }
            }
        }

        var maxSceneFile = GetMaxSceneFile(NamedOutputDirectory);
        Logger.Info("Extracted");
        Logger.Info($"Scene file: {maxSceneFile}; Target directory: {UnityAssetsResultDirectory}");

        var launcher = new ProcessLauncher(TdsMaxPlugin.Path)
        {
            Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
            ThrowOnStdErr = false,
            ThrowOnNonZeroExitCode = false,
            Arguments =
            {
                // minimized, dialog boxes suppressed
                "-ma", "-silent",

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
                maxSceneFile.Replace('\\', '/'),
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

        var unityTemplateNames = new[] { "OCURP21+", /*"OCHDRP22+"*/ };
        foreach (var unityTemplateName in unityTemplateNames)
            await tryProcess(unityTemplateName);


        UnityBakedExportInfo readExportInfoFromFileName(string path)
        {
            // r1_2014_[2021.3.32f1]_[URP]_[51]
            path = Path.GetFileNameWithoutExtension(path);

            var spt = path.Split('[', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new UnityBakedExportInfo(
                parse(spt[3]),
                parse(spt[1]),
                parse(spt[2]),
                null
            );


            string parse(string str)
            {
                var index = str.IndexOf(']', StringComparison.Ordinal);
                if (index == -1) throw new Exception($"Could not parse export info from filename {path}");

                return str.Substring(0, index);
            }
        }
        bool readImporterVersionFile(string path, [MaybeNullWhen(false)] out UnityBakedExportInfo importerVersion)
        {
            importerVersion = null;
            if (!File.Exists(path))
                return false;

            var infofilecontents = File.ReadAllText(path);
            var infofile = infofilecontents.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (infofile.Length < 3 || infofile.Length > 4)
            {
                Logger.Error($"Invalid {Path.GetFileName(path)}: \n" + infofilecontents);
                return false;
            }

            importerVersion = new(infofile[0], infofile[1], infofile[2], infofile.Length >= 4 && !string.IsNullOrWhiteSpace(infofile[3]) ? infofile[3] : null);
            return true;
        }
        async Task tryProcess(string unityTemplateName)
        {
            var ocImporterVersion = null as UnityBakedExportInfo;

            try
            {
                Logger.Info($"Trying {ZipFilePath} {unityTemplateName}");

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

                throw new Exception($"Could not process {unityTemplateName} for {ZipFilePath}: {ex.Message}", ex);
            }


            bool needsConversion()
            {
                if (ExportInfo.Unity is null || !ExportInfo.Unity.TryGetValue(unityTemplateName, out var info) || info is null)
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
            var (importerVersion, unityVersion, rendererType, launchArgs) = ocImporterVersion;

            var productName = ProductName;
            var assetsResultDir = UnityAssetsSceneResultDirectory;
            var completeResultDir = Path.Combine(ResultDir, productName);

            if (Path.Exists(completeResultDir))
            {
                void cleanup(string dir)
                {
                    dir = Path.Combine(completeResultDir, dir);
                    if (!Directory.Exists(dir)) return;

                    foreach (var render in Directory.GetFiles(dir))
                    {
                        try
                        {
                            var info = readExportInfoFromFileName(render);
                            if (info.ImporterVersion != importerVersion)
                            {
                                Logger.Info($"Deleting {render} as it's not of latest version");
                                File.Delete(render);
                            }
                        }
                        catch { }
                    }
                }

                cleanup("Builds");
                cleanup("renders");
                cleanup("scenes");
                Directories.Merge(completeResultDir, assetsResultDir);
            }


            Logger.Info($"Importing with Unity {unityVersion} {rendererType} and importer v{importerVersion}");
            var unity = PluginList.GetPlugin(PluginType.Unity, unityVersion);

            foreach (var fbx in Directory.GetFiles(unityTemplateAssetsDir, "*.fbx"))
            {
                Logger.Info($"Deleting already completed file {fbx}");
                File.Delete(fbx);

                var dir = Path.ChangeExtension(fbx, null);
                if (Directory.Exists(dir))
                {
                    Logger.Info($"Deleting already completed dir {dir}");
                    Directory.Delete(dir, true);
                }

                var metafile = Path.ChangeExtension(fbx, ".meta");
                if (File.Exists(metafile))
                {
                    Logger.Info($"Deleting already completed meta {metafile}");
                    File.Delete(metafile);
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

                var unityLogFile = UnityLogFile;
                if (File.Exists(unityLogFile))
                    File.Delete(unityLogFile);

                launchArgs ??= string.Join(' ', new[]
                {
                    "-accept-apiupdate",
                    "-batchmode",
                    "-executeMethod", "OCBatchScript.StartBake",
                    "-noLM",
                });

                //NonAdminRunner.RunAsDesktopUserWaitForExit(unity.Path, );
                var launcher = new ProcessLauncher(unity.Path)
                {
                    Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
                    ThrowOnStdErr = false,
                    ThrowOnNonZeroExitCode = false,
                    Timeout = TimeSpan.FromMinutes(10),
                    Arguments =
                    {
                        launchArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(arg => arg.Replace("\"", "")),
                        "-projectPath", unityTemplateDir,
                        "-logFile", unityLogFile,
                    },
                };
                await launcher.ExecuteAsync();

                await Task.Delay(100);
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
                var buildResultDir = Path.Combine(unityImportResultDir, "Builds", $"{productName}_[{unityVersion}]_[{rendererType}]_[{importerVersion}]");
                if (!Directory.Exists(buildResultDir))
                {
                    Logger.Error($"{buildResultDir} was not found; searching for an empty dir");
                    try
                    {
                        buildResultDir = Directory.GetDirectories(Path.Combine(unityImportResultDir, "Builds"))
                            .Where(dir => Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0)
                            .Single();
                    }
                    catch { Directory.CreateDirectory(buildResultDir); }
                }


                if (Directory.Exists(buildResultDir))
                    Directory.Delete(buildResultDir, true);

                {
                    var dest = Directories.DirCreated(completeResultDir, "Builds");

                    var moved = false;
                    for (int i = 0; i < 60; i++)
                    {
                        var exeprocess = Process.GetProcesses().Where(proc =>
                        {
                            try { return Path.GetFileName(proc.MainModule?.FileName)?.StartsWith(productName) == true; }
                            catch { return false; }
                        }).FirstOrDefault();

                        if (exeprocess is not null)
                        {
                            if (i == 60 - 1)
                                exeprocess.Kill();
                            else
                            {
                                Logger.Info($"Found the exported app exe running: {exeprocess.Id} {exeprocess.MainModule!.FileName}; waiting 5 sec");
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
                            Logger.Info($"Could not move the build dir {buildProjectDir} to {dest}: {ex.Message}; retrying after 5 sec");
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
                        Logger.Info($"Moving the build dir from {buildProjectDir} to {dest}");
                        if (Directory.Exists(dest))
                            Directory.Delete(dest, true);

                        Directory.Move(buildProjectDir, dest);
                    }
                }
            }
            finally
            {
                moveBack();
            }

            Logger.Info($"Moving result dir {assetsResultDir} to {ResultDir}");
            Directories.Merge(assetsResultDir, completeResultDir);

            Logger.Info($"Moving fbx {Path.Combine(assetsResultDir, Path.GetFileName(assetsResultDir) + ".fbx")} to {Path.Combine(completeResultDir, Path.GetFileName(completeResultDir) + ".fbx")}");
            File.Move(Path.ChangeExtension(assetsResultDir, ".fbx"), Path.Combine(completeResultDir, Path.GetFileName(completeResultDir) + ".fbx"));

            Task.Run(async () => await ReportResult(productName)).Consume();
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


    public record UnityBakedExportInfo(string ImporterVersion, string UnityVersion, string RendererType, string? LaunchArgs);

    public record OneClickProjectExportInfo(string Version, bool Successful);
    public record UnityProjectExportInfo(string ImporterVersion, string UnityVersion, string RendererType, string ImporterCommitHash, bool Successful);
    public class ProjectExportInfo
    {
        public OneClickProjectExportInfo? OneClick { get; set; }
        public Dictionary<string, UnityProjectExportInfo>? Unity { get; set; }
    }
}
