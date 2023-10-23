namespace SoftwareRegistry.Controllers;

[ApiController]
[Route("soft")]
public class SoftwareController : ControllerBase
{
    readonly SoftwareList SoftList;

    public SoftwareController(SoftwareList softList) => SoftList = softList;

    [HttpGet("get")]
    public JObject GetSoftware() =>
        JsonApi.Success(
            JObject.FromObject(
                SoftList.AllSoftware
                    .GroupBy(s => s.Type)
                    .ToDictionary(g => g.Key, g => g.ToDictionary(s => s.Version)),
                JsonSettings.LowercaseS
            )
        );


    [HttpGet("gettorrent")]
    public ActionResult GetTorrent([FromQuery] PluginType plugin, [FromQuery] string version)
    {
        if (SoftList.TryGet(plugin, version, out var soft))
            return File(soft.TorrentFileBytes, "application/x-bittorrent");

        return StatusCode(StatusCodes.Status404NotFound);
    }
}
