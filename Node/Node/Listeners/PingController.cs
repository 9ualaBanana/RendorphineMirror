using Microsoft.AspNetCore.Mvc;

namespace Node.Listeners;

[ApiController]
public class PingController : ControllerBase
{
    [HttpGet("ping")]
    public OkResult Ping() => Ok();

    [HttpGet("who")]
    public OkObjectResult Who([FromServices] Init init) =>
        Ok(JsonApi.Success($"ok from {Environment.MachineName} {Environment.UserName} {Settings.NodeName} v{init.Version} port {Settings.UPnpPort}"));

    [HttpGet("getpublicpagesport")]
    public OkObjectResult GetPublicPagesPort()
    {
        Response.Headers.AccessControlAllowOrigin = "*";
        return Ok(JsonApi.Success(Settings.UPnpPort));
    }
}
