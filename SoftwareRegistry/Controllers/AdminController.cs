using System.Formats.Tar;

namespace SoftwareRegistry.Controllers;

[ApiController]
[Route("admin")]
[SessionIdAuthorization]
public class AdminController : ControllerBase
{
    readonly SoftwareList SoftList;
    readonly DataDirs Dirs;

    public AdminController(SoftwareList softList, DataDirs dirs)
    {
        SoftList = softList;
        Dirs = dirs;
    }

    [HttpPost("delete")]
    public async Task<JObject> Delete([FromQuery] PluginType plugin, [FromQuery] string version)
    {
        var removed = await SoftList.RemoveAsync(plugin, version);
        if (!removed) return JsonApi.Error("Plugin version was not found");

        return JsonApi.Success();
    }

    [HttpGet("download")]
    public async Task<ActionResult<JObject>> Download([FromQuery] PluginType type, [FromQuery] string version)
    {
        if (!SoftList.TryGet(type, version, out var info))
            return NotFound();

        var ms = new MemoryStream();
        await TarFile.CreateFromDirectoryAsync(info.Directory, ms, false);
        ms.Position = 0;

        return File(ms.ToArray(), "application/x-tar", fileDownloadName: $"{type}_{version}");
    }

    [HttpPost("uploadtar")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<JObject>> UploadTar([FromForm] IFormFile file)
    {
        if (file.Headers.ContentType != "application/x-tar")
            return new UnsupportedMediaTypeResult();

        using var _ = Directories.DisposeDelete(Dirs.NamedTempDir("upload/" + Guid.NewGuid()), out var dir);
        using (var stream = file.OpenReadStream())
            await TarFile.ExtractToDirectoryAsync(stream, dir, true);

        var added = await SoftList.TryAddNewPlugin(dir);
        return JsonApi.JsonFromOpResult(added);
    }
}
