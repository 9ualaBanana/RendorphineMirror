using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json.Serialization;
using Common;
using NLog;
using UpdaterCommon;
using UpdateServer;

Initializer.ConfigDirectory = "renderfin-updater";
Init.Initialize();
var logger = LogManager.GetCurrentClassLogger();
var filez = new ConcurrentDictionary<string, Dictionary<string, UpdaterFileInfo>>();


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
                if (!File.Exists(file)) filez.TryRemove(file, out _);
                else UpdateFileData(file);
            }

            logger.Info($"Hashes rebuilt.");
        });
    };

    watcher.EnableRaisingEvents = true;
    return watcher;


    void UpdateFileData(string file)
    {
        file = Path.GetFullPath(Path.Combine(basedir, file));

        var app = Path.GetRelativePath(basedir, file);
        app = app.Substring(0, app.IndexOf('/', StringComparison.Ordinal));

        if (!filez.TryGetValue(app, out var list))
            filez[app] = list = new();

        var data = GetActualFileData(file, app);
        list[data.Path] = data;
    }

    IEnumerable<string> GetFilesRecursive(string dir)
    {
        dir = Path.GetFullPath(Path.Combine(basedir, dir));
        return get(dir).Select(x => Path.GetRelativePath(dir, x));


        IEnumerable<string> get(string dir) => Directory.GetFiles(dir).Concat(Directory.GetDirectories(dir).SelectMany(get));
    }
    UpdaterFileInfo GetActualFileData(string path, string app)
    {
        GetInfo(path, out var hash, out var size, out var time);
        return new(Path.GetRelativePath(Path.Combine(basedir, app), path), time, size, hash);


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
                // gzip stores uncompressed file length in last 4 bytes but its broken for >4G so we decompress instead
                using var file = File.OpenRead(path);
                using var gzip = new GZipStream(file, CompressionMode.Decompress);

                hash = XXHash.XXH64(gzip, out size);

                File.WriteAllText(hashfile, hash.ToString());
                File.SetLastWriteTimeUtc(hashfile, filetime);

                File.WriteAllText(sizefile, size.ToString());
                File.SetLastWriteTimeUtc(sizefile, filetime);

                logger.Info($"[Hash recalc] {path}: {hash}");
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

    return Results.Ok(new Return<IEnumerable<UpdaterFileInfo>>(1, data.Values));
}
async ValueTask Download(string path, string app, HttpRequest request, HttpResponse response, CancellationToken token)
{
    LogRequest(request);
    await WaitWhileUpdating();

    if (!filez.TryGetValue(app, out var files) || !files.TryGetValue(path, out var file))
    {
        response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    response.StatusCode = StatusCodes.Status200OK;
    response.Headers.ContentEncoding = "gzip";

    using var zipfile = File.Open(Path.Combine(basedir, app, file.Path), FileMode.Open, FileAccess.Read, FileShare.Read);
    response.Headers.ContentLength = zipfile.Length;

    await zipfile.CopyToAsync(response.Body).ConfigureAwait(false);
}
void DoLog(HttpRequest request, HttpResponse response)
{
    LogRequest(request);

    response.StatusCode = StatusCodes.Status200OK;
    response.Body.Close();
}


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