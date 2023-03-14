using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Web;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace UpdaterCommon;

/* Update() method execution
    search for updates
    if everything is already updated
        delete tempdir
        return success

    download updates to tempdir
    /\ if tempdir exists then include its files in hash comparison, so if a previous update was terminated it won't start from zero

    restart into new updater in tempdir + elevate
    /\ copy current updater into tempdir if updater wasn't updated

    in the new updater:
    kill everything
    move files from tempdir to targetdir
    kill everything again just in case
    start appexes
*/
public class UpdateChecker
{
    static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    [JsonIgnore]
    readonly HttpClient Client = new(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip }) { Timeout = TimeSpan.FromMinutes(1), };


    public event Action FetchingStarted = delegate { };
    public event Action FilteringStarted = delegate { };
    public event Action<IReadOnlyList<UpdaterFileInfo>> DownloadingStarted = delegate { };
    public event Action<int> BytesDownloaded = delegate { };
    public event Action<string> FileDownloaded = delegate { };
    public event Action StartingApp = delegate { };


    public readonly string Url, App;
    public readonly string TargetDirectory, TempDirectory;
    public readonly ImmutableArray<string> AppExecutables;
    public readonly ImmutableDictionary<string, string> Args;

    public static UpdateChecker LoadFromJsonOrDefault(string? url = null, string? app = null, string? targetdirectory = null, string? tempdirectory = null, string[]? appexecutables = null, IReadOnlyDictionary<string, string>? args = null)
    {
        if (!File.Exists("updater.json")) return new(url, app, targetdirectory, tempdirectory, appexecutables, args);

        var jsontext = File.ReadAllText("updater.json");
        return JsonConvert.DeserializeObject<UpdateChecker>(jsontext).ThrowIfNull("Could not deserialize updater.json");
    }
    public UpdateChecker(string? url = null, string? app = null, string? targetdirectory = null, string? tempdirectory = null, string[]? appexecutables = null, IReadOnlyDictionary<string, string>? args = null)
    {
        Url = url ?? "https://t.microstock.plus:5011";

        if (app is null)
        {
            app = "renderfin";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) app += "-win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) app += "-lin";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) app += "-mac";
        }
        App = app;

        if (targetdirectory is null)
        {
            var dir = App;
            if (dir.Contains('-', StringComparison.OrdinalIgnoreCase))
                dir = string.Join('-', dir.Split('-')[..^1]);

            var folder = (Initializer.UseAdminRights && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? Environment.SpecialFolder.ProgramFiles : Environment.SpecialFolder.LocalApplicationData;
            targetdirectory = Path.Combine(Environment.GetFolderPath(folder), dir);
        }
        targetdirectory = Path.GetFullPath(targetdirectory);
        TargetDirectory = targetdirectory;
        Directory.CreateDirectory(TargetDirectory);

        TempDirectory = tempdirectory ?? Path.Combine(Path.GetTempPath(), $"{app}-update");
        Directory.CreateDirectory(TempDirectory);

        appexecutables ??= new[] { "Node", "NodeUI" };
        for (int i = 0; i < appexecutables.Length; i++)
        {
            ref var appexe = ref appexecutables[i];

            if (!appexe.EndsWith(".exe", StringComparison.Ordinal) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                appexe += ".exe";
            appexe = Path.Combine(TargetDirectory, appexe);
        }
        AppExecutables = appexecutables.ToImmutableArray();

        Args = args?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty;

        LogManager.GetCurrentClassLogger().Info($"Creating default updater: {JsonConvert.SerializeObject(this)}");
    }


    static string PcInfo() => HttpUtility.UrlEncode($"{File.GetLastWriteTimeUtc(Environment.ProcessPath!).ToString("ddMMyy_hhmm")} {Environment.MachineName}_{Environment.UserName}");
    public ValueTask<OperationResult<UpdaterFileInfo[]>> GetAllFiles() =>
        Api.Default.ApiGet<UpdaterFileInfo[]>(@$"{Url}/getfiles", "value", "Getting files", ("app", App), ("pc", PcInfo()));

    public UpdaterFileInfo[] FilterNewFiles(IReadOnlyCollection<UpdaterFileInfo> files) => FilterNewFiles(new[] { TargetDirectory, TempDirectory }, files);
    public static UpdaterFileInfo[] FilterNewFiles(IReadOnlyCollection<string> dirs, IReadOnlyCollection<UpdaterFileInfo> files)
    {
        var sw = Stopwatch.StartNew();
        var fs = files.AsParallel().Where(check).ToArray();
        _logger.Info($"Filtered {files.Count} -> {fs.Length} files in {sw.ElapsedMilliseconds} ms");

        return fs;


        bool check(UpdaterFileInfo file)
        {
            var path = null as string;
            foreach (var dir in dirs)
            {
                var f = Path.Combine(dir, file.Path);
                if (File.Exists(f))
                {
                    path = f;
                    break;
                }
            }
            if (path is null) return true;

            //if (new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds() != file.ModificationTime)
            //    return true;
            if (new FileInfo(path).Length != file.Size)
                return true;

            return XXHash.XXH64(path) != file.Hash;
        }
    }


    /// <returns> Error if error; Success if no updates found; Environment.Exit(0) if updates found </returns>
    public async ValueTask<OperationResult> Update()
    {
        _logger.Info("Checking files to update...");
        FetchingStarted();
        var files = await GetAllFiles().Next(x => { FilteringStarted(); return FilterNewFiles(x).AsOpResult(); }).ConfigureAwait(false);
        if (!files) return files.GetResult();
        if (files.Value.Length == 0)
        {
            if (Directory.Exists(TempDirectory))
                Directory.Delete(TempDirectory, true);

            return true;
        }

        _logger.Info($"Downloading {files.Value.Length} files");
        if (files.Value.Length < 10)
            _logger.Info(string.Join("; ", files.Value.Select(x => x.Path)));

        DownloadingStarted(files.Value);
        var download = await files.Value.OrderByDescending(x => x.Size).Select(DownloadFileToTemp).MergeParallel(6).ConfigureAwait(false);
        if (!download) return download;

        startNewUpdater();
        return true;


        [DoesNotReturn]
        void startNewUpdater()
        {
            string newUpdaterExe;
            if (!File.Exists(newUpdaterExe = Path.Combine(TempDirectory, "Updater")) && !File.Exists(newUpdaterExe = Path.Combine(TempDirectory, "Updater.exe")))
            {
                var updaterexe = FileList.GetUpdaterExe();
                File.Copy(updaterexe, newUpdaterExe = Path.Combine(TempDirectory, Path.GetFileName(updaterexe)));
            }

            CommonExtensions.MakeExecutable(newUpdaterExe);
            var start = new ProcessStartInfo(newUpdaterExe) { WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = false };
            foreach (var arg in Environment.GetCommandLineArgs().Skip(1))
                start.ArgumentList.Add(arg);
            start.ArgumentList.Add(JObject.FromObject(this).With(j => j["doupdate"] = true).ToString(Formatting.None));

            if (Initializer.UseAdminRights && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // elevate

                start.UseShellExecute = true;
                start.Verb = "runas";
            }

            _logger.Info($"Restarting to {start.FileName}");
            var process = Process.Start(start);
            if (process is null) throw new InvalidOperationException("Could not start the updater");
            Environment.Exit(0);
        }
    }
    public async ValueTask<OperationResult> DownloadFileToTemp(UpdaterFileInfo file)
    {
        try
        {
            var localfilename = Path.Combine(TempDirectory, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(localfilename)!);

            // using HttpClient instead of Api for gzip decompression
            using (var stream = await Client.GetStreamAsync($"{Url}/download?pc={PcInfo()}&app={App}&path={HttpUtility.UrlEncode(file.Path)}").ConfigureAwait(false))
            using (var writer = new CallbackStream(File.OpenWrite(localfilename)) { OnWrite = count => BytesDownloaded(count) })
                await stream.CopyToAsync(writer).ConfigureAwait(false);

            File.SetLastWriteTimeUtc(localfilename, DateTimeOffset.FromUnixTimeSeconds(file.ModificationTime).UtcDateTime);
            _logger.Info($"+ {file.Path} ({(new FileInfo(localfilename).Length / 1024 / 1024d).ToString("0.##")} MB)");

            FileDownloaded(file.Path);
            return true;
        }
        catch (Exception ex) { return OperationResult.Err(ex); }
    }

    public void MoveFilesAndStartApp()
    {
        FileList.KillProcesses(TargetDirectory);
        Common.CommonExtensions.MergeDirectories(TempDirectory, TargetDirectory);
        FileList.KillProcesses(TargetDirectory);
        StartApp();
    }
    public void StartApp(bool skipRunning = false)
    {
        StartingApp();
        Common.CommonExtensions.MakeExecutable(TargetDirectory);

        foreach (var appexe in AppExecutables)
        {
            if (skipRunning && FileList.IsProcessRunning(Path.GetFullPath(appexe))) continue;

            Args.TryGetValue(Path.GetFileNameWithoutExtension(appexe), out var args);
            _logger.Info($"Starting {appexe} {args}");

            Process.Start(new ProcessStartInfo(appexe, args ?? string.Empty)
            {
                WorkingDirectory = Path.GetDirectoryName(appexe),
                // WindowStyle = ProcessWindowStyle.Hidden,
            }).ThrowIfNull($"Application process is null after starting ({appexe})");
        }
    }


    class CallbackStream : Stream
    {
        public override bool CanRead => BaseStream.CanRead;
        public override bool CanSeek => BaseStream.CanSeek;
        public override bool CanWrite => BaseStream.CanWrite;
        public override long Length => BaseStream.Length;
        public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }

        public Action<int>? OnWrite;
        public readonly Stream BaseStream;

        public CallbackStream(Stream baseStream) => BaseStream = baseStream;

        public override void Flush() => BaseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);
        public override void SetLength(long value) => BaseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);
            OnWrite?.Invoke(count);
        }


        protected override void Dispose(bool disposing)
        {
            BaseStream.Dispose();
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            await BaseStream.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}