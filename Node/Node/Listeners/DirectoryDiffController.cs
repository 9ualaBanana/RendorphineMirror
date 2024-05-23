using Microsoft.AspNetCore.Mvc;

namespace Node.Listeners;

[ApiController]
public class DirectoryDiffController : ControllerBase
{
    [HttpGet("dirdiff")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetDiff([FromQuery] string path, [FromQuery] int lastcheck = 0)
    {
        var dir = path;
        // TODO: whitelist for directories or something?

        if (!Directory.Exists(dir))
            return Ok(JsonApi.Error("Directory does not exists"));

        var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Select(file => new DiffOutputFile(file, new FileInfo(file).Length, new DateTimeOffset(System.IO.File.GetCreationTimeUtc(file)).ToUnixTimeMilliseconds()))
            .Where(v => v.ModifTime > lastcheck)
            .ToImmutableArray();

        return Ok(JsonApi.Success(new DiffOutput(files)));
    }

    [HttpGet("download")]
    [SessionIdAuthorization]
    public PhysicalFileResult Download([FromQuery] string path) =>
        PhysicalFile(path, MimeTypes.GetMimeType(path), System.IO.Path.GetFileName(path));

    public readonly record struct DiffOutput(ImmutableArray<DiffOutputFile> Files);
    public readonly record struct DiffOutputFile(string Path, long Size, long ModifTime);
}
