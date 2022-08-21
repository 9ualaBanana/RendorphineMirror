global using System.Collections.Immutable;
global using Common;
global using NLog;
using Microsoft.AspNetCore.Mvc;
using MonoTorrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.Web;
using SoftwareRegistry;
using Body = Microsoft.AspNetCore.Mvc.FromBodyAttribute;
using Query = Microsoft.AspNetCore.Mvc.FromQueryAttribute;
using Srv = Microsoft.AspNetCore.Mvc.FromServicesAttribute;

Initializer.ConfigDirectory = "renderphin_registry";

var logger = LogManager.GetCurrentClassLogger();

Directory.CreateDirectory("torrents");
foreach (var dir in Directory.GetDirectories("torrents"))
{
    var torrentfile = Path.ChangeExtension(dir, ".torrent");
    if (!File.Exists(torrentfile))
    {
        var bytes = await TorrentClient.CreateTorrent(dir);
        await File.WriteAllBytesAsync(torrentfile, bytes);
    }

    var torrent = await Torrent.LoadAsync(torrentfile);
    await TorrentClient.AddOrGetTorrent(torrent, dir);
    logger.Info("Started torrent " + torrent.InfoHash);
}
logger.Info("Torrent listening at :" + TorrentClient.DhtPort);



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
    public JObject GetPeerId() => JsonApi.Success(new JsonPeer(TorrentClient.PeerId.UrlEncode(), TorrentClient.DhtPort));

    [HttpGet("getsoft")]
    public JObject GetSoftware([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist) => JsonApi.Success(softlist.Software);


    [HttpPost("addsoft")]
    public JObject AddSoftware([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Body] SoftwareDefinition soft)
    {
        softlist.Add(soft);
        return JsonApi.Success(softlist.Software);
    }

    [HttpPost("delsoft")]
    public JObject DeleteSoftware([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name)
    {
        var data = GetSoft(softlist, name)
            .Next(ver =>
            {
                softlist.Remove(ver);
                return softlist.AsOpResult();
            });

        return JsonApi.JsonFromOpResult(data);
    }

    [HttpPost("delver")]
    public JObject DeleteVersion([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name, [Query] string version)
    {
        var data = GetSoft(softlist, name, version, out var soft)
            .Next(ver =>
            {
                softlist.Replace(soft, soft with { Versions = soft.Versions.Remove(ver) });
                return softlist.AsOpResult();
            });

        return JsonApi.JsonFromOpResult(data);
    }

    [HttpPost("editsoft")]
    public JObject EditSoftware([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name, [Body] JObject soft)
    {
        var data = GetSoft(softlist, name)
            .Next(prev =>
            {
                var copy = prev with { };
                using (var reader = soft.CreateReader())
                    new JsonSerializer().Populate(reader, copy);

                softlist.Replace(prev, copy);
                return softlist.AsOpResult();
            });

        return JsonApi.JsonFromOpResult(data);
    }

    [HttpPost("editver")]
    public JObject EditVersion([Srv] ILogger<SoftwareController> logger, [Srv] SoftList softlist,
        [Query] string name, [Query] string version, [Body] JObject soft)
    {
        var data = GetSoft(softlist, name, version, out var prevsoft)
            .Next(prev =>
            {
                var copy = prev with { };
                using (var reader = soft.CreateReader())
                    new JsonSerializer().Populate(reader, copy);

                softlist.Replace(prevsoft, prevsoft with { Versions = prevsoft.Versions.Replace(prev, copy) });
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
        var soft = softlist.Software.FirstOrDefault(x => x.TypeName.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (soft is null) return OperationResult.Err("Software does not exists");

        return soft;
    }
    OperationResult<SoftwareVersionDefinition> GetSoft(SoftList softlist, string name, string version, out SoftwareDefinition soft)
    {
        var softr = GetSoft(softlist, name);
        soft = softr.Value;
        if (!softr) return softr.EString;

        var ver = softr.Value.Versions.FirstOrDefault(x => x.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
        if (ver is null) return OperationResult.Err("Version does not exists");

        return ver;
    }
}