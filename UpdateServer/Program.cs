using System.IO.Compression;
using System.Text.Json.Serialization;
using UpdaterCommon;
using UpdateServer;

Initializer.AppName = "renderfin-updater";
Init.Initialize();
var logger = LogManager.GetCurrentClassLogger();
var appz = new Dictionary<string, AppData>();


var builder = WebApplication.CreateBuilder(args);

var config = builder.Configuration.Get<Config>();
var url = config.Url;
var urlbase = config.UrlBase;
var basedir = Path.GetFullPath(config.BaseDirectory);
var hashbasedir = Path.GetFullPath(config.HashBaseDirectory);
Directory.CreateDirectory(basedir);
Directory.CreateDirectory(hashbasedir);

logger.Info($"Starting server @ {url}/{urlbase} with {string.Join(", ", Directory.GetDirectories(basedir))}");

var updating = false;
using var watcher = StartFileWatcher();

builder.WebHost.UseUrls(url);
builder.Services.AddControllers().AddNewtonsoftJson();
var wapp = builder.Build();

wapp.MapGet(urlbase + "/getfiles", GetFiles);
wapp.MapGet(urlbase + "/download", Download);
wapp.MapGet(urlbase + "/log", DoLog);
wapp.Run();


FileSystemWatcher StartFileWatcher()
{
    foreach (var file in GetFilesRecursive(basedir))
        UpdateFileData(file);

    var delay = new ResettableDelay();
    var changed = new List<string>(20);
    var watcher = new FileSystemWatcher(basedir) { IncludeSubdirectories = true };

    watcher.Changed += (obj, e) =>
    {
        logger.Info($"[FS updates] {e.FullPath}");
        lock (changed) changed.Add(e.FullPath);

        delay.ExecuteAfter(TimeSpan.FromSeconds(10), () =>
        {
            string[] arr;
            lock (changed)
            {
                arr = changed.Distinct().ToArray();
                changed.Clear();
            }
            logger.Info($"Rebuilding {arr.Length}+ files...");

            using var __ = new FuncDispose(() => updating = false);
            updating = true;

            foreach (var file in arr.SelectMany(x => Directory.Exists(x) ? Directory.GetFiles(x, "*", SearchOption.AllDirectories) : new[] { x }))
                UpdateFileData(Path.GetRelativePath(Path.GetFullPath(basedir), file));

            logger.Info($"Hashes rebuilt.");
        });
    };

    watcher.EnableRaisingEvents = true;
    return watcher;


    IEnumerable<string> GetFilesRecursive(string dir)
    {
        dir = Path.GetFullPath(Path.Combine(basedir, dir));
        return get(dir).Select(x => Path.GetRelativePath(dir, x));


        IEnumerable<string> get(string dir) => Directory.GetFiles(dir).Concat(Directory.GetDirectories(dir).SelectMany(get));
    }
}
void UpdateFileData(string fileRelativeToBaseDir)
{
    // renderfin-win.assets/Updater.exe
    // fileRelativeToBaseDir

    // renderfin-win.assets
    var appdir = fileRelativeToBaseDir.Substring(0, fileRelativeToBaseDir.IndexOf('/', StringComparison.Ordinal));

    // renderfin-win
    var appname = appdir.Split('.')[0];

    // Updater.exe
    var fileRelativeToAppDir = Path.GetRelativePath(appdir, fileRelativeToBaseDir);

    // files/renderfin-win.assets/Updater.exe
    var fullpath = Path.Combine(basedir, fileRelativeToBaseDir);

    // hashes/renderfin-win.assets/Updater.exe
    var dataFileBasePath = Path.Combine(hashbasedir, fileRelativeToBaseDir);
    Directory.CreateDirectory(Path.GetDirectoryName(dataFileBasePath)!);

    // hashes/renderfin-win.assets/Updater.exe._hash_
    var hashfile = dataFileBasePath + "._hash_";

    // hashes/renderfin-win.assets/Updater.exe._size_
    var sizefile = dataFileBasePath + "._size_";


    if (!appz.TryGetValue(appname, out var app))
        appz[appname] = app = new(appname, new(), new());

    if (!File.Exists(fullpath))
    {
        app.Files.Remove(fileRelativeToAppDir);
        app.FileDirs.Remove(fileRelativeToAppDir);
        if (File.Exists(hashfile)) File.Delete(hashfile);
        if (File.Exists(sizefile)) File.Delete(sizefile);

        return;
    }

    GetInfo(out var hash, out var size, out var time);
    app.Files[fileRelativeToAppDir] = new(fileRelativeToAppDir, time, size, hash);
    app.FileDirs[fileRelativeToAppDir] = appdir;


    void GetInfo(out ulong hash, out long size, out long time)
    {
        var filetime = File.GetLastWriteTimeUtc(fullpath);
        time = new DateTimeOffset(filetime).ToUnixTimeSeconds();

        if (File.Exists(hashfile) && File.GetLastWriteTimeUtc(hashfile) == filetime
            && File.Exists(sizefile) && File.GetLastWriteTimeUtc(sizefile) == filetime)
        {
            hash = ulong.Parse(File.ReadAllText(hashfile));
            size = long.Parse(File.ReadAllText(sizefile));
        }
        else
        {
            try { hash = calculateHash(out size); }
            catch (InvalidDataException)
            {
                var temp = Path.GetTempFileName();
                using (var tempfile = File.OpenWrite(temp))
                using (var gzip = new GZipStream(tempfile, CompressionMode.Compress))
                using (var file = File.OpenRead(fullpath))
                    file.CopyTo(gzip);

                File.Move(temp, fullpath, true);
                hash = calculateHash(out size);
            }

            // gzip stores uncompressed file length in last 4 bytes but its broken for >4G so we decompress instead
            ulong calculateHash(out long size)
            {
                using var file = File.OpenRead(fullpath);
                using var gzip = new GZipStream(file, CompressionMode.Decompress);

                return XXHash.XXH64(gzip, out size);
            }

            File.WriteAllText(hashfile, hash.ToString());
            File.SetLastWriteTimeUtc(hashfile, filetime);

            File.WriteAllText(sizefile, size.ToString());
            File.SetLastWriteTimeUtc(sizefile, filetime);

            logger.Info($"[Hash recalc] {fileRelativeToBaseDir}: {hash}");
        }
    }
}


async ValueTask WaitWhileUpdating()
{
    while (updating)
        await Task.Delay(100);
}
void LogRequest(HttpRequest request)
{
    logger.Info(@$"{Pad(22, request.HttpContext.Connection.RemoteIpAddress).Substring("::ffff:".Length)}:{Pad(5, request.HttpContext.Connection.RemotePort)} {request.Method} {request.Path}{request.QueryString}");

    static string Pad<T>(int amount, T text) where T : notnull
    {
        var str = text.ToString() ?? "";
        return str + new string(' ', Math.Max(0, amount - str.Length));
    }
}

async ValueTask<IResult> GetFiles(string app, HttpRequest request)
{
    LogRequest(request);
    await WaitWhileUpdating();

    if (!appz.TryGetValue(app, out var data))
        return Results.NotFound();

    return Results.Ok(new Return<IEnumerable<UpdaterFileInfo>>(1, data.Files.Values));
}
async ValueTask Download(string path, string app, HttpRequest request, HttpResponse response, CancellationToken token)
{
    LogRequest(request);
    await WaitWhileUpdating();


    if (!appz.TryGetValue(app, out var info) || !info.Files.TryGetValue(path, out var file))
    {
        response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    // renderfin-win.assets/Updater.exe
    var pathRelativeToBaseDir = Path.Combine(info.FileDirs[file.Path], file.Path);
    var actualPath = Path.Combine(basedir, pathRelativeToBaseDir);
    if (!File.Exists(actualPath))
    {
        UpdateFileData(pathRelativeToBaseDir); // just to remove it
        response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    response.StatusCode = StatusCodes.Status200OK;
    response.Headers.ContentEncoding = "gzip";

    using var zipfile = File.Open(Path.Combine(basedir, actualPath), FileMode.Open, FileAccess.Read, FileShare.Read);
    response.Headers.ContentLength = zipfile.Length;

    await zipfile.CopyToAsync(response.Body).ConfigureAwait(false);
}
void DoLog(HttpRequest request, HttpResponse response)
{
    LogRequest(request);

    response.StatusCode = StatusCodes.Status200OK;
    response.Body.Close();
}


record AppData(string Name, Dictionary<string, string> FileDirs, Dictionary<string, UpdaterFileInfo> Files);
class Config
{
    public string Url { get; set; } = null!;
    public string UrlBase { get; set; } = null!;
    public string BaseDirectory { get; set; } = null!;
    public string HashBaseDirectory { get; set; } = null!;
}
readonly struct Return<T>
{
    [JsonPropertyName("ok")] public int Ok { get; }
    [JsonPropertyName("value")] public T Value { get; }

    public Return(int ok, T value)
    {
        Ok = ok;
        Value = value;
    }
}