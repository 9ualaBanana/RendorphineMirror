using System.IO.Compression;
using System.Text.Json.Serialization;
using Common;
using NLog;
using UpdaterCommon;
using UpdateServer;

Initializer.ConfigDirectory = "renderfin-updater";
Init.Initialize();
var logger = LogManager.GetCurrentClassLogger();
var filez = new Dictionary<string, AppData>();


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
            {
                if (!File.Exists(file)) filez.Remove(file);
                else UpdateFileData(file);
            }

            logger.Info($"Hashes rebuilt.");
        });
    };

    watcher.EnableRaisingEvents = true;
    return watcher;


    void UpdateFileData(string relafile)
    {
        var appdir = Path.GetRelativePath(basedir, Path.GetFullPath(Path.Combine(basedir, relafile)));
        appdir = relafile.Substring(0, relafile.IndexOf('/', StringComparison.Ordinal));
        var appname = appdir.Split('.')[0];

        if (!filez.TryGetValue(appdir, out var app))
            filez[appname] = app = new(appname, new(), new(), new());

        app.Subdirs.Add(appdir);

        var data = GetActualFileData(relafile, appdir);
        app.Files[data.Path] = data;
        app.FileDirs[data.Path] = appdir;
    }

    IEnumerable<string> GetFilesRecursive(string dir)
    {
        dir = Path.GetFullPath(Path.Combine(basedir, dir));
        return get(dir).Select(x => Path.GetRelativePath(dir, x));


        IEnumerable<string> get(string dir) => Directory.GetFiles(dir).Concat(Directory.GetDirectories(dir).SelectMany(get));
    }
    UpdaterFileInfo GetActualFileData(string path, string appdir)
    {
        GetInfo(path, out var hash, out var size, out var time);
        return new(Path.GetRelativePath(Path.Combine(basedir, appdir), Path.Combine(basedir, path)), time, size, hash);


        void GetInfo(string path, out ulong hash, out long size, out long time)
        {
            path = Path.Combine(basedir, path);

            var filetime = File.GetLastWriteTimeUtc(path);
            time = new DateTimeOffset(filetime).ToUnixTimeSeconds();

            var datapath = Path.Combine(hashbasedir, Path.GetRelativePath(basedir, path));
            Directory.CreateDirectory(Path.GetDirectoryName(datapath)!);

            var hashfile = datapath + "._hash_";
            var sizefile = datapath + "._size_";

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
                    using (var file = File.OpenRead(path))
                        file.CopyTo(gzip);

                    File.Move(temp, path, true);
                    hash = calculateHash(out size);
                }

                // gzip stores uncompressed file length in last 4 bytes but its broken for >4G so we decompress instead

                ulong calculateHash(out long size)
                {
                    using var file = File.OpenRead(path);
                    using var gzip = new GZipStream(file, CompressionMode.Decompress);

                    return XXHash.XXH64(gzip, out size);
                }

                File.WriteAllText(hashfile, hash.ToString());
                File.SetLastWriteTimeUtc(hashfile, filetime);

                File.WriteAllText(sizefile, size.ToString());
                File.SetLastWriteTimeUtc(sizefile, filetime);

                logger.Info($"[Hash recalc] {Path.GetRelativePath(basedir, path)}: {hash}");
            }
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

    if (!filez.TryGetValue(app, out var data))
        return Results.NotFound();

    return Results.Ok(new Return<IEnumerable<UpdaterFileInfo>>(1, data.Files.Values));
}
async ValueTask Download(string path, string app, HttpRequest request, HttpResponse response, CancellationToken token)
{
    LogRequest(request);
    await WaitWhileUpdating();


    if (!filez.TryGetValue(app, out var info) || !info.Files.TryGetValue(path, out var file))
    {
        response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    response.StatusCode = StatusCodes.Status200OK;
    response.Headers.ContentEncoding = "gzip";

    using var zipfile = File.Open(Path.Combine(basedir, info.FileDirs[file.Path], file.Path), FileMode.Open, FileAccess.Read, FileShare.Read);
    response.Headers.ContentLength = zipfile.Length;

    await zipfile.CopyToAsync(response.Body).ConfigureAwait(false);
}
void DoLog(HttpRequest request, HttpResponse response)
{
    LogRequest(request);

    response.StatusCode = StatusCodes.Status200OK;
    response.Body.Close();
}


record AppData(string Name, HashSet<string> Subdirs, Dictionary<string, string> FileDirs, Dictionary<string, UpdaterFileInfo> Files);
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