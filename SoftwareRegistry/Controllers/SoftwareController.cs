namespace SoftwareRegistry.Controllers;

[ApiController]
[Route("soft")]
[SessionIdAuthorization]
public class SoftwareController : ControllerBase
{
    readonly SoftList SoftList;

    public SoftwareController(SoftList softList) => SoftList = softList;


    [HttpGet("get")]
    [AllowAnonymous]
    public JObject GetSoftware() => JsonApi.Success(SoftList.Software);


    [HttpPost("set")]
    public JObject Set([FromQuery] string name, [FromBody] SoftwareDefinition? soft = null)
    {
        if (soft is null) SoftList.Remove(name);
        else SoftList.Replace(name, soft);

        return JsonApi.Success(SoftList.Software);
    }

    [HttpPost("setall")]
    public JObject SetAll([FromBody] ImmutableDictionary<string, SoftwareDefinition> soft)
    {
        SoftList.Set(soft);
        return GetSoftware();
    }
}
