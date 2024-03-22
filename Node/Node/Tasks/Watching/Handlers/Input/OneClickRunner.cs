using System.IO.Compression;
using System.Text.RegularExpressions;
using _3DProductsPublish._3DProductDS;
using SevenZip;

namespace Node.Tasks.Watching.Handlers.Input;

public class OneClickRunner : OneClickRunnerInfo
{
    public required Action SaveFunc { get; init; }
    public required OneClickWatchingTaskInputHandler.OCLocalListener LocalListener { get; init; }

    public required WatchingTask WatchingTask { get; init; }
    public required IPluginList PluginList { get; init; }
    public required Plugin TdsMaxPlugin { get; init; }
    public required Plugin OneClickPlugin { get; init; }
    public required IRFProductStorage RFProducts { get; init; }
    public required ILogger Logger { get; init; }

    public OneClickRunner(OneClickWatchingTaskInputInfo input, bool test) : base(input, test) { }

    public async Task Run()
    {
        Directory.CreateDirectory(OutputDir);
        await InstallOneClickIfNeeded();

        var archives = Directory.GetFiles(InputDir, "*.zip")
            .Concat(Directory.GetFiles(InputDir, "*.rar"))
            .Order()
            .Chunk(1);

        foreach (var task in archives)
            await RunChunk(task);
    }

    void RecursiveExtract(string archive, string destination)
    {
        if (Directory.Exists(destination))
            Directory.Delete(destination, true);

        // required for 7zextractor as it replaces / with _
        destination = Path.GetFullPath(destination);

        Logger.Info($"Extracting {archive} to {destination}");
        using (var extractor = new SevenZipExtractor(archive))
            extractor.ExtractArchive(destination);

        while (true)
        {
            var archives = Directory.GetFiles(destination, "*.zip", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(destination, "*.rar", SearchOption.AllDirectories))
                .ToArray();

            if (archives.Length == 0) break;

            foreach (var zip in archives)
            {
                var dest = Path.GetDirectoryName(zip)!;

                Logger.Info($"Extracting {zip} to {dest}");
                using (var extractor = new SevenZipExtractor(zip))
                    extractor.ExtractArchive(dest);
                File.Delete(zip);
            }
        }
    }

    string GetExportInfoForLog()
    {
        var exportInfos = GetExportInfosByArchiveFiles(Directory.GetFiles(InputDir));

        var all = exportInfos.Length;
        var oneclickcompleted = exportInfos.Count(r => r.OneClick is { Successful: true });
        var unityfullcompleted = exportInfos.Count(r => r.Unity?.All(u => u.Value.Successful) == true);

        return $"""
            [ {all} input zip; {oneclickcompleted} 3dsmax export completed; {unityfullcompleted} unity import completed ]
            """;
    }
    async Task ReportError(string msg, IEnumerable<string> inputArchiveFiles)
    {
        try
        {
            var header = string.Empty;

            try
            {
                var dir = Directory.GetFiles(Path.Combine(OutputDir, "*[UNITY*")).Max();
                if (dir is not null)
                {
                    var logfiles = Directory.GetFiles(dir, "*.log", SearchOption.AllDirectories)
                        .Append(Directory.GetFiles(Path.Combine(LogDir, "unity")).Max())
                        .WhereNotNull()
                        .ToArray();

                    if (logfiles.Length != 0)
                    {
                        var ip = await PortForwarding.GetPublicIPAsync();

                        header += "\nUnity logs:";
                        header += string.Join(string.Empty, logfiles.Select(log => $"\nhttp://{ip}:{Settings.UPnpServerPort}/oc/unitylog?file={Path.GetFileNameWithoutExtension(log)}"));
                    }
                }
            }
            catch { }

            msg = $"""
                Error processing {string.Join(", ", inputArchiveFiles.Select(Path.GetFileNameWithoutExtension))}
                {header}
                {GetExportInfoForLog()}

                ```
                {msg.Replace(@"\", @"\\")}
                ```
                """;

            Logger.Info($"Reporting an error {msg}");
            var query = Api.ToQuery(("error", msg));
            //await Api.Default.ApiPost($"{Settings.ServerUrl}/oneclick/display_render_error?{query}", "Displaying render error", content: null);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending a message to tg bot: {ex}");
        }
    }
    async Task ReportResult(ProductJson productInfo)
    {
        using var content = new MultipartFormDataContent();

        foreach (var renderfile in productInfo.VideoPreviews)
            content.Add(new StreamContent(File.OpenRead(renderfile)), "renders", Path.GetFileName(renderfile));

        var msg = $"""
            {Settings.NodeName} converted
            ```json
            {JsonConvert.SerializeObject(productInfo)}
            ```
            {GetExportInfoForLog()}
            """;

        Logger.Info($"Reporting a result {msg} with files {string.Join(", ", productInfo.VideoPreviews)}");
        var query = Api.ToQuery(("caption", msg));
        //await Api.Default.ApiPost($"{Settings.ServerUrl}/oneclick/display_renders?{query}", "Displaying renders", content);
    }

    async Task RunChunk(IReadOnlyList<string> inputArchiveFiles)
    {
        if (WatchingTask.IsPaused) return;

        foreach (var inputArchiveFile in inputArchiveFiles)
        {
            try
            {
                await RunMax(inputArchiveFile);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await ReportError($"[3dsmax export] {ex.Message}", new[] { inputArchiveFile });
            }
        }

        if (false)
        {
            try
            {
                await RunUnity(inputArchiveFiles);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await ReportError($"[Unity import] {ex.Message}", inputArchiveFiles);
            }
            finally
            {
                SaveFunc();
            }
        }
    }

    async Task RunMax(string inputArchiveFile)
    {
        using var _ = Logger.BeginScope($"3dsmax");

        var output3dsmaxdir = Achive3dsMaxExtractDirectory(inputArchiveFile);
        if (Directory.Exists(output3dsmaxdir))
        {
            try
            {
                var ei = GetExportInfoByProductName(Path.GetFileNameWithoutExtension(GetMaxSceneFile(output3dsmaxdir)));
                if (ei.OneClick?.Version == OneClickPlugin.Version)
                    return;

                ei.OneClick = null;
                Directory.Delete(output3dsmaxdir, true);
            }
            catch { }
        }

        // kill previous 3dsmax
        try
        {
            if (Input.Launched3dsMaxProcessId is { } id)
            {
                Logger.Info($"Killing 3dsmax (pid {id})");
                var prevproc = Process.GetProcessById(id);

                Logger.Info($"Found process ({prevproc.ProcessName})");
                if (prevproc.ProcessName != "3dsmax.exe")
                    throw new Exception("Not a 3dsmax.exe");

                prevproc.Kill(true);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Couldn't kill previous 3dsmax process: {ex.Message}");
        }

        RecursiveExtract(inputArchiveFile, output3dsmaxdir);
        Logger.Info("Extracted");

        var maxSceneFile = GetMaxSceneFile(output3dsmaxdir);

        if (!Directory.Exists(Input.ProductsDirectory))
            Logger.Info($"Product directory {Input.ProductsDirectory} doesn't exists, not copying settings.ini");
        else
        {
            var settingsIniFile = Path.Combine(Input.ProductsDirectory, $"{Path.GetFileNameWithoutExtension(maxSceneFile)}_Settings.ini");
            if (!File.Exists(settingsIniFile))
                Logger.Info($"Settings.ini file {settingsIniFile} doesn't exists, not copying");
            else
            {
                var target = Path.Combine(Path.GetDirectoryName(maxSceneFile)!, Path.GetFileNameWithoutExtension(maxSceneFile), Path.GetFileName(settingsIniFile));
                Logger.Info($"Copying Settings.ini {settingsIniFile} to {target}");
                File.Copy(settingsIniFile, target);
            }
        }

        var exportInfo = GetExportInfoByProductName(Path.GetFileNameWithoutExtension(maxSceneFile));
        exportInfo.OneClick = null;
        SaveFunc();


        var outputunitydir = Directories.DirCreated(Export3dsMaxResultDirectory(inputArchiveFile));
        Logger.Info($"Scene file: {maxSceneFile}; Target directory: {outputunitydir}");

        var launcher = new ProcessLauncher(TdsMaxPlugin.Path)
        {
            Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
            ThrowOnStdErr = false,
            ThrowOnNonZeroExitCode = false,
            Timeout = TimeSpan.FromHours(2),
            Arguments =
            {
                // minimized, dialog boxes suppressed
                "-ma", "-silent",

                // log path
                "-log", Path.Combine(LogDir, Path.GetFileNameWithoutExtension(maxSceneFile) + ".log").With(f => { if (File.Exists(f)) File.Delete(f); }),

                // script parameters
                /*
                int - target engine; 1 = unreal, 2 = unity
                string - output dir; should already exist
                int - existing texture mode; 1 = skip copying, 2 = 256px, 3=512, 4=1024, 5=2048, 6=4096
                int - bake texture mode; 1 = skip baking, 2 = 128px, 3=256, 4=512, 5=1024, 6=2048, 7=4096
                bool int int - render cameras (true\false) and frame width height (always should be specified)
                (1.35+) bool - deploy importer from internal zip
                */
                "-mxs", $"oneclickexport.oc000 2 @\"{outputunitydir}\" 6 7 true 960 540 false",

                // scene to export
                maxSceneFile.Replace('\\', '/'),
            },
        };

        Logger.Info("Launching 3dsmax");
        using var proc = launcher.Start(out var procReadingTask);
        Input.Launched3dsMaxProcessId = proc.Id;
        SaveFunc();
        await launcher.WaitForEnd(proc, procReadingTask);
        Input.Launched3dsMaxProcessId = null;
        SaveFunc();
        Logger.Info("Conversion completed");

        Logger.Info("Validating");
        try
        {
            await validateConversionSuccessful();
        }
        catch
        {
            exportInfo.OneClick = new(OneClickPlugin.Version.ToString(), false);
            SaveFunc();
            throw;
        }

        Logger.Info("Success.");
        exportInfo.OneClick = new(OneClickPlugin.Version.ToString(), true);
        SaveFunc();


        async Task validateConversionSuccessful()
        {
            Logger.Info("Validating conversion");

            if (!Directory.Exists(outputunitydir))
                throw new Exception($"Result directory {outputunitydir} does not exists");

            var logfiles = Directory.GetDirectories(outputunitydir)
                .Select(dir => Path.Combine(dir, Path.GetFileName(dir) + ".txt"))
                .Where(File.Exists)
                .ToArray();

            if (logfiles.Length == 0)
                throw new Exception($"Log file was not found in {outputunitydir}");

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
    async Task RunUnity(IReadOnlyList<string> inputArchiveFiles)
    {
        using var _ = Logger.BeginScope($"Unity");
        var exportInfos = GetExportInfosByArchiveFiles(inputArchiveFiles);
        var newUnityTemplatesCommitHash = await UpdateUnityTemplates();

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
            if (infofile.Length is < 3 or > 4)
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
                Logger.Info($"Trying to process {unityTemplateName} for [{string.Join(", ", exportInfos.Select(info => info.ProductName))}]");

                // C:\OneClickUnityDefaultProjects\{OCURP21+}
                var unityTemplateDir = Path.Combine(UnityTemplatesDir, unityTemplateName);

                // C:\OneClickUnityDefaultProjects\{OCURP21+}\Assets
                var unityTemplateAssetsDir = Path.Combine(unityTemplateDir, "Assets");

                // C:\OneClickUnityDefaultProjects\{OCURP21+}\Assets\OCImporterVersion.txt
                var ocImporterVersionFile = Path.Combine(unityTemplateAssetsDir, "OCImporterVersion.txt");

                if (!readImporterVersionFile(ocImporterVersionFile, out ocImporterVersion))
                    return;

                if (!exportInfos.Any(needsConversion))
                    return;

                await process(unityTemplateName, unityTemplateDir, unityTemplateAssetsDir, ocImporterVersion);
            }
            catch (Exception ex)
            {
                if (ocImporterVersion is not null)
                    foreach (var exportInfo in exportInfos)
                        (exportInfo.Unity ??= new())[unityTemplateName] = new(ocImporterVersion.ImporterVersion, ocImporterVersion.UnityVersion, ocImporterVersion.RendererType, newUnityTemplatesCommitHash, null);

                var msg = $"""
                    Importer info: {JsonConvert.SerializeObject((ocImporterVersion as object) ?? "unknown")}
                    Could not process {unityTemplateName} for [{string.Join(", ", exportInfos.Select(info => info.ProductName))}]: {ex.Message}
                    """;
                throw new Exception(msg, ex);
            }


            bool needsConversion(ProjectExportInfo exportInfo)
            {
                // OneClick
                {
                    // convert only if either:
                    // - not converted
                    // - versions are different

                    if (exportInfo.OneClick is null)
                        return true;

                    if (exportInfo.OneClick.Version != OneClickPlugin.Version)
                        return true;
                }

                // Unity
                {
                    // convert only if either:
                    // - not converted
                    // - versions are different
                    // - versions are the same but commit hashes are different and the previous convertion was not successful

                    if (exportInfo.Unity is null || !exportInfo.Unity.TryGetValue(unityTemplateName, out var unityinfo) || unityinfo is null)
                        return true;

                    if (unityinfo.ImporterVersion != ocImporterVersion.ImporterVersion)
                        return true;

                    if (unityinfo.ImporterCommitHash != newUnityTemplatesCommitHash && !unityinfo.Successful)
                        return true;
                }

                return false;
            }
        }
        async Task process(string unityTemplateName, string unityTemplateDir, string unityTemplateAssetsDir, UnityBakedExportInfo ocImporterVersion)
        {
            try
            {
                var (importerVersion, unityVersion, rendererType, launchArgs) = ocImporterVersion;
                Logger.Info($"Importing with Unity {unityVersion} {rendererType} and importer v{importerVersion}");

                await killUnity();
                async Task killUnity()
                {
                    Logger.Info("Killing unity");
                    await new ProcessLauncher("taskkill", "/IM", "Unity.exe", "/F") { ThrowOnStdErr = false, ThrowOnNonZeroExitCode = false }
                        .ExecuteAsync();
                }

                Logger.Info("Launching unity");

                var unityLogFile = Directories.NumberedNameInDirectory(Directories.DirCreated(LogDir, "unity"), "log{0:0000}.log");
                if (File.Exists(unityLogFile))
                    File.Delete(unityLogFile);

                launchArgs ??= string.Join(' ', new[]
                {
                    "-accept-apiupdate",
                    "-batchmode",
                    "-executeMethod", "OCBatchScript.StartBake",
                    "-noLM",
                });

                var launcher = new ProcessLauncher(PluginList.GetPlugin(PluginType.Unity, unityVersion).Path)
                {
                    Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
                    ThrowOnStdErr = false,
                    ThrowOnNonZeroExitCode = false,
                    Timeout = TimeSpan.FromMinutes(5),
                    Arguments =
                    {
                        launchArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(arg => arg.Replace("\"", "")),
                        "-projectPath", unityTemplateDir,
                        "-logFile", unityLogFile,
                    },
                };

                var killswitch = new CancellationTokenSource();

                var execution = launcher.ExecuteAsync(killswitch.Token);
                var reading = Task.Run(async () =>
                {
                    try
                    {
                        await LocalListener.WaitForCompletion((product) => add(product).Consume(), TimeSpan.FromMinutes(5), killswitch.Token);


                        async Task add(ProductJson product)
                        {
                            // _[2021.3.32f1]_[URP]_[85]
                            var version = product.OCVersion;

#pragma warning disable SYSLIB1045 // Use 'GeneratedRegexAttribute' to generate the regular expression implementation at compile-time.
                            var matches = Regex.Matches(version, @"\[[^\]]*\]")
#pragma warning restore SYSLIB1045
                                .Cast<Match>()
                                .ToArray();

                            var unityVersion = matches[0].Value[1..^1];
                            var rendererType = matches[1].Value[1..^1];
                            var importerVersion = matches[2].Value[1..^1];

                            (GetExportInfoByProductName(product.OCPName).Unity ??= new())[unityTemplateName] = new(importerVersion, unityVersion, rendererType, newUnityTemplatesCommitHash, product);
                            Task.Run(async () => await ReportResult(product)).Consume();
                        }
                    }
                    catch
                    {
                        killswitch.Cancel();
                    }
                });

                await Task.WhenAll(execution, reading);
                killswitch.Cancel();

                Logger.Info("Completed");
            }
            finally
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (process.MainModule?.FileName.StartsWith(OutputDir, StringComparison.Ordinal) != true)
                            continue;
                    }
                    catch { continue; }

                    try
                    {
                        Logger.Info($"Killing {process.Id}");
                        process.Kill();
                    }
                    catch { }
                }
            }
        }
    }

    async Task<string> UpdateUnityTemplates()
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
                Logging = { ILogger = Logger, LogStartMessage = false, StdErr = Microsoft.Extensions.Logging.LogLevel.Trace, StdOut = Microsoft.Extensions.Logging.LogLevel.Trace },
                ThrowOnStdErr = false,
                WorkingDirectory = Path.GetFullPath(UnityTemplatesDir),
            }.ExecuteFullAsync();
        }
    }

    async Task InstallOneClickIfNeeded()
    {
        var currentOneClickVersion = Directory.GetFiles(OutputDir, "*.mzp")
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault()
            ?.Substring("oneclickexport.v".Length);

        if (currentOneClickVersion == OneClickPlugin.Version)
            return;

        await InstallOneClick();
        await CheckOneClickInstallation();
        await MoveOneClickOldVersion(currentOneClickVersion);
    }
    async Task InstallOneClick()
    {
        Logger.Info("Installing the plugin");

        // fix for vray not being silent enough
        const string vraySilentFix = "if setVRaySilentMode != undefined then setVRaySilentMode()";
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(TdsMaxPlugin.Path).ThrowIfNull(), "scripts", "Startup", "oneclicksilent.ms"), vraySilentFix);

        foreach (var process in Process.GetProcessesByName("3dsmax"))
        {
            try { process.Kill(); }
            catch { }
        }

        var launcher = new ProcessLauncher(TdsMaxPlugin.Path)
        {
            Logging = new ProcessLauncher.ProcessLogging() { ILogger = Logger, },
            ThrowOnStdErr = false,
            ThrowOnNonZeroExitCode = false,
            Timeout = TimeSpan.FromMinutes(5),
            Arguments = { "-ms", "-silent", "-mxs", $"fileIn @\"{OneClickPlugin.Path}\"" },
        };
        await launcher.ExecuteAsync();

        Logger.Info("Plugin installed");
    }
    async Task CheckOneClickInstallation()
    {
        Logger.Info("Checking plugin installation");

        using var reader = File.OpenRead(OneClickPlugin.Path);
        var entry = new ZipArchive(reader).GetEntry("oneclickreadme.txt").ThrowIfNull("OneClick version was not found in mzp");
        using var entrystream = new StreamReader(entry.Open());
        var expectedversion = await entrystream.ReadToEndAsync();

        // %localAppData%\Autodesk\3dsMax\20?? - 64bit\ENU\scripts\startup\oneclickreadme.txt
        var installedpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Autodesk", "3dsMax", $"{TdsMaxPlugin.Version} - 64bit", "ENU", "scripts", "startup", "oneclickreadme.txt");

        var installedversion = File.ReadAllText(installedpath);

        if (installedversion != expectedversion)
            throw new Exception($"Invalid mzp installation: versions are not equal (installed {installedversion} vs expected {expectedversion})");

        Logger.Info($"Installed plugin version: {installedversion}");
    }
    async Task MoveOneClickOldVersion(string? currentversion)
    {
        var output = OutputDir;
        Logger.Info($"Moving old dir {output}");

        Logger.Info($"Deleting old RFProducts {output}");
        foreach (var dir in Directory.GetDirectories(OutputDir))
            foreach (var product in RFProducts.GetProductsWithContainerAt(dir).SelectMany(p => p.GetSubProductsRecursive()))
            {
                Logger.Info($"Deleting old RFProduct {product.ID} {product.Path}");
                product.Delete();
                RFProducts.RFProducts.Remove(product.ID);
            }

        if (Directory.Exists(output))
        {
            if (currentversion is null) Directory.Delete(output, true);
            else Directory.Move(output, output + currentversion);
        }

        Directory.CreateDirectory(output);
        var target = Path.Combine(output, Path.GetFileName(OneClickPlugin.Path));
        File.Copy(OneClickPlugin.Path, target);


        Logger.Info($"Old output dir moved to {target}");
    }
}
