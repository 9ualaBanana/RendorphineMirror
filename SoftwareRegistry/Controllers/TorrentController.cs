namespace SoftwareRegistry.Controllers;

[ApiController]
[Route("torrent")]
public class TorrentController : ControllerBase
{
    [HttpGet("get")]
    public IActionResult Get([FromServices] TorrentHolder torrentholder, [FromBody] string plugin, [FromQuery] string version)
    {
        if (torrentholder.TryGet(plugin, version, out var bytes))
            return File(bytes, "application/x-bittorrent");

        return StatusCode(StatusCodes.Status404NotFound);
    }
}
