using Microsoft.AspNetCore.Authorization;

namespace SoftwareRegistry.Controllers;

[ApiController]
[SessionIdAuthorization]
public class SoftwareController : ControllerBase
{
    [HttpGet("getsoft")]
    [AllowAnonymous]
    public JObject GetSoftware([FromServices] ILogger<SoftwareController> logger, [FromServices] SoftList softlist,
        [FromQuery] string? name = null, [FromQuery] string? version = null)
    {
        if (name is null) return JsonApi.Success(softlist.Software);
        if (version is null) return JsonApi.JsonFromOpResult(GetSoft(softlist, name));

        return JsonApi.JsonFromOpResult(GetSoft(softlist, name, version, out _));
    }


    [HttpPost("addsoft")]
    public JObject AddSoftware([FromServices] ILogger<SoftwareController> logger, [FromServices] SoftList softlist,
        [FromQuery] string name, [FromBody] SoftwareDefinition soft)
    {
        softlist.Add(name, soft);
        return JsonApi.Success(softlist.Software);
    }

    [HttpPost("addver")]
    public JObject AddVersion([FromServices] ILogger<SoftwareController> logger, [FromServices] SoftList softlist,
        [FromQuery] string name, string version, [FromBody] SoftwareVersionDefinition ver)
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
    public JObject DeleteSoftware([FromServices] ILogger<SoftwareController> logger, [FromServices] SoftList softlist,
        [FromQuery] string name)
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
    public JObject DeleteVersion([FromServices] ILogger<SoftwareController> logger, [FromServices] SoftList softlist,
        [FromQuery] string name, [FromQuery] string version)
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
    public JObject EditAll([FromServices] ILogger<SoftwareController> logger, [FromServices] SoftList softlist,
        [FromBody] JObject soft)
    {
        softlist.Set(soft.ToObject<ImmutableDictionary<string, SoftwareDefinition>>() ?? throw new InvalidOperationException());
        return GetSoftware(logger, softlist);
    }

    [HttpPost("editsoft")]
    public JObject EditSoftware([FromServices] ILogger<SoftwareController> logger, [FromServices] SoftList softlist,
        [FromQuery] string name, [FromBody] JObject soft, [FromQuery] string? newname = null)
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
    public JObject EditVersion([FromServices] ILogger<SoftwareController> logger, [FromServices] SoftList softlist,
        [FromQuery] string name, [FromQuery] string version, [FromBody] JObject soft, [FromQuery] string? newversion = null)
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
