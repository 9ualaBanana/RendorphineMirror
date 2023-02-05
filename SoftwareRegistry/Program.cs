global using System.Collections.Immutable;
global using Common;
global using NLog;
global using NodeCommon;
using Microsoft.AspNetCore.Mvc;
using MonoTorrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.Web;
using SoftwareRegistry;
using Body = Microsoft.AspNetCore.Mvc.FromBodyAttribute;
using Query = Microsoft.AspNetCore.Mvc.FromQueryAttribute;
using Srv = Microsoft.AspNetCore.Mvc.FromServicesAttribute;

Initializer.ConfigDirectory = "renderfin_registry";
Init.Initialize();

var logger = LogManager.GetCurrentClassLogger();

Directory.CreateDirectory("torrents");
foreach (var dir in Directory.GetDirectories("torrents").Concat(Directory.GetDirectories("torrents").SelectMany(Directory.GetDirectories)))
{
    if (Directory.GetFiles(dir).Length == 0) continue;

    var torrentfile = Path.Combine("torrents", Path.GetRelativePath("torrents", dir.EndsWith('/') ? dir[..^1] : dir).Replace('/', '.') + ".torrent");
    if (!File.Exists(torrentfile))
    {
        logger.Info("Creating torrent from " + dir);

        var bytes = await TorrentClient.CreateTorrent(dir);
        await File.WriteAllBytesAsync(torrentfile, bytes);
    }

    var torrent = await Torrent.LoadAsync(torrentfile);
    var manager = await TorrentClient.AddOrGetTorrent(torrent, dir);
    logger.Info("Started torrent " + torrentfile + " " + torrent.InfoHash.ToHex());
}
logger.Info("Torrent listening at dht" + TorrentClient.DhtPort + " and trt" + TorrentClient.ListenPort);

foreach (var t in TorrentClient.Client.Torrents)
    await t.LocalPeerAnnounceAsync();



var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSingleton(new SoftList());

var app = builder.Build();

app.MapControllers();
app.Run();




[Controller]
public class SoftwareController : ControllerBase
{
    [HttpGet("getpeer")]
    public JObject GetPeerId() => JsonApi.Success(new JsonPeer(TorrentClient.PeerId.UrlEncode(), ImmutableArray.Create(TorrentClient.ListenPort)));

    [HttpGet("getsoft")]
    public JObject GetSoftware([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string? name = null, [Query] string? version = null)
    {
        if (name is null) return JsonApi.Success(softlist.Software);
        if (version is null) return JsonApi.JsonFromOpResult(GetSoft(softlist, name));

        return JsonApi.JsonFromOpResult(GetSoft(softlist, name, version, out _));
    }


    [HttpPost("addsoft")]
    public JObject AddSoftware([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name, [Body] SoftwareDefinition soft)
    {
        softlist.Add(name, soft);
        return JsonApi.Success(softlist.Software);
    }

    [HttpPost("addver")]
    public JObject AddVersion([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name, string version, [Body] SoftwareVersionDefinition ver)
    {
        var data = GetSoft(softlist, name)
            .Next(soft =>
            {
                softlist.Replace(name, soft with { Versions = soft.Versions.Add(version, ver) });
                return softlist.AsOpResult();
            });

        return JsonApi.Success(softlist.Software);
    }

    [HttpGet("delsoft")]
    public JObject DeleteSoftware([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name)
    {
        var data = GetSoft(softlist, name)
            .Next(soft =>
            {
                softlist.Remove(name);
                return softlist.AsOpResult();
            });

        return JsonApi.JsonFromOpResult(data);
    }

    [HttpGet("delver")]
    public JObject DeleteVersion([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name, [Query] string version)
    {
        var data = GetSoft(softlist, name, version, out var soft)
            .Next(ver =>
            {
                softlist.Replace(name, soft with { Versions = soft.Versions.Remove(version) });
                return softlist.AsOpResult();
            });

        return JsonApi.JsonFromOpResult(data);
    }

    [HttpPost("editall")]
    public JObject EditAll([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Body] JObject soft)
    {
        softlist.Set(soft.ToObject<ImmutableDictionary<string, SoftwareDefinition>>() ?? throw new InvalidOperationException());
        return GetSoftware(logger, softlist);
    }

    [HttpPost("editsoft")]
    public JObject EditSoftware([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name, [Body] JObject soft, [Query] string? newname = null)
    {
        var data = GetSoft(softlist, name)
            .Next(prev =>
            {
                var copy = prev with { };
                using (var reader = soft.CreateReader())
                    new JsonSerializer().Populate(reader, copy);

                softlist.Replace(name, copy, newname);
                return softlist.AsOpResult();
            });

        return JsonApi.JsonFromOpResult(data);
    }

    [HttpPost("editver")]
    public JObject EditVersion([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name, [Query] string version, [Body] JObject soft, [Query] string? newversion = null)
    {
        var data = GetSoft(softlist, name, version, out var prevsoft)
            .Next(prev =>
            {
                var copy = prev with { };
                using (var reader = soft.CreateReader())
                    new JsonSerializer().Populate(reader, copy);

                softlist.Replace(name, prevsoft with { Versions = prevsoft.Versions.Remove(version).SetItem(newversion ?? version, copy) });
                return softlist.AsOpResult();
            });

        return JsonApi.JsonFromOpResult(data);
    }


    [HttpGet("gettorrent")]
    public ActionResult GetTorrent([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string plugin)
    {
        var path = Path.GetFullPath(Path.Combine("torrents", plugin + ".torrent"));
        if (!path.StartsWith(Path.GetFullPath("torrents"), StringComparison.Ordinal) || !System.IO.File.Exists(path))
            return StatusCode(404);

        return PhysicalFile(path, "application/x-bittorrent");
    }




    OperationResult<SoftwareDefinition> GetSoft(SoftList softlist, string name)
    {
        if (!softlist.TryGetValue(name, out var soft))
            return OperationResult.Err("Software does not exists");

        return soft;
    }
    OperationResult<SoftwareVersionDefinition> GetSoft(SoftList softlist, string name, string version, out SoftwareDefinition soft)
    {
        var softr = GetSoft(softlist, name);
        soft = softr.Value;
        if (!softr) return softr.EString;

        if (!soft.Versions.TryGetValue(version, out var ver))
            return OperationResult.Err("Version does not exists");

        return ver;
    }
}