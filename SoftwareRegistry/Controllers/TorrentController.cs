using System.Formats.Tar;

namespace SoftwareRegistry.Controllers;

[ApiController]
[Route("torrent")]
[SessionIdAuthorization]
public class TorrentController : ControllerBase
{
    readonly TorrentManager TorrentManager;

    public TorrentController(TorrentManager torrentManager) => TorrentManager = torrentManager;


    [HttpGet("get")]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] string plugin, [FromQuery] string version)
    {
        if (TorrentManager.TryGetBytes(plugin, version, out var bytes))
            return File(bytes, "application/x-bittorrent");

        return StatusCode(StatusCodes.Status404NotFound);
    }

    [HttpGet("getlist")]
    public JObject Get()
    {
        var result = Directory.GetDirectories(TorrentManager.TorrentsDirectory)
            .Select(d => new { plugin = Path.GetFileName(d), versions = Directory.GetDirectories(d).Select(Path.GetFileName) });

        return JsonApi.Success(result);
    }

    [HttpGet("delete")]
    public async Task<JObject> Delete([FromQuery] string plugin, [FromQuery] string version)
    {
        await TorrentManager.DeleteAsync(plugin, version);
        return JsonApi.Success();
    }

    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<JObject>> Upload([FromQuery] string plugin, [FromQuery] string version, [FromForm] IFormFile file)
    {
        if (file.Headers.ContentType != "application/x-tar")
            return new UnsupportedMediaTypeResult();

        await TorrentManager.DeleteAsync(plugin, version);
        Directory.CreateDirectory(TorrentManager.DirectoryFor(plugin, version));

        using (var stream = file.OpenReadStream())
            await TarFile.ExtractToDirectoryAsync(stream, TorrentManager.DirectoryFor(plugin, version), true);

        await TorrentManager.AddAsync(plugin, version);

        return JsonApi.Success();
    }
}
