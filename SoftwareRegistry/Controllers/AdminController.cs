using System.Formats.Tar;
using System.Text;

namespace SoftwareRegistry.Controllers;

[ApiController]
[Route("admin")]
[SessionIdAuthorization]
public class AdminController : ControllerBase
{
    readonly byte[] AdminPage;
    readonly SoftwareList SoftList;
    readonly DataDirs Dirs;

    public AdminController(SoftwareList softList, DataDirs dirs)
    {
        SoftList = softList;
        Dirs = dirs;

        using var adminpagestream = typeof(AdminController).Assembly.GetManifestResourceStream("SoftwareRegistry.Resources.admin.html").ThrowIfNull("Admin page was not found");
        using var reader = new StreamReader(adminpagestream);
        AdminPage = Encoding.UTF8.GetBytes(reader.ReadToEnd());
    }

    [HttpGet()]
    public FileResult Root() =>
        File(AdminPage, "text/html; charset=utf-8");
    // for debug
    // File(Encoding.UTF8.GetBytes(System.IO.File.ReadAllText("Resources/admin.html")), "text/html; charset=utf-8");

    [HttpGet("delete")]
    public async Task<JObject> Delete([FromQuery] PluginType type, [FromQuery] string version)
    {
        var removed = await SoftList.RemoveAsync(type, version);
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

    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<JObject>> Upload([FromForm] IFormFile file)
    {
        if (file.Headers.ContentType != "application/x-tar")
            return new UnsupportedMediaTypeResult();

        using var _ = Directories.DisposeDelete(Dirs.NamedTempDir("upload/" + Guid.NewGuid()), out var dir);
        using (var stream = file.OpenReadStream())
            await TarFile.ExtractToDirectoryAsync(stream, dir, true);

        var added = await SoftList.TryAddNewPlugin(dir);
        return JsonApi.JsonFromOpResult(added);
    }

    [HttpPost("fileupload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<JObject>> FileUpload(
        [FromForm] string type,
        [FromForm] string name,
        [FromForm] string version,
        [FromForm] string? files_enabled, // null | 'on'
        [FromForm] string? files_main,
        [FromForm] string? installation_enabled, // null | 'on'
        [FromForm] string? installation_source,
        [FromForm] string? installation_script,
        [FromForm] string? installation_python_enabled, // null | 'on'
        [FromForm] string? installation_python_version,
        [FromForm] string? installation_python_pip_requirements,
        [FromForm] string? installation_python_pip_requirementfiles,
        [FromForm] string? installation_python_conda_requirements,
        [FromForm] string? installation_python_conda_channels,
        [FromForm] string? requirements_platforms,
        [FromForm] string? requirements_parents,
        [FromForm] IFormFile file
    )
    {
        var info = new SoftwareVersionInfo(
            Enum.Parse<PluginType>(type),
            version,
            name,
            files_enabled is null or not "on"
                ? null
                : new SoftwareVersionInfo.FilesInfo(
                    files_main.ThrowIfNullOrEmpty()
                ),
            installation_enabled is null or not "on"
                ? null
                : new SoftwareVersionInfo.InstallationInfo(
                    new SoftwareVersionInfo.InstallationInfo.SourceInfo(Enum.Parse<SoftwareVersionInfo.InstallationInfo.SourceInfo.SourceType>(installation_source.ThrowIfNullOrEmpty())),
                    string.IsNullOrEmpty(installation_script) ? null : installation_script,
                    installation_python_enabled is null or not "on"
                        ? null
                        : new SoftwareVersionInfo.InstallationInfo.PythonInfo(
                            installation_python_version.ThrowIfNullOrEmpty(),
                            new SoftwareVersionInfo.InstallationInfo.PythonInfo.PipInfo(
                                (installation_python_pip_requirements ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToImmutableArray(),
                                (installation_python_pip_requirementfiles ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToImmutableArray()
                            ),
                            new SoftwareVersionInfo.InstallationInfo.PythonInfo.CondaInfo(
                                (installation_python_conda_requirements ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToImmutableArray(),
                                (installation_python_conda_channels ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToImmutableArray()
                            )
                        )
                ),
                new SoftwareVersionInfo.RequirementsInfo(
                    (requirements_platforms ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToImmutableArray(),
                    (requirements_parents ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(p =>
                        {
                            var str = p.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            return new SoftwareVersionInfo.RequirementsInfo.ParentInfo(Enum.Parse<PluginType>(str[0]), str.Length == 1 ? null : str[1]);
                        })
                        .ToImmutableArray()
                )
        );

        using var _ = Directories.DisposeDelete(Dirs.NamedTempDir("upload/" + Guid.NewGuid()), out var dir);

        using (var filestream = System.IO.File.Create(Path.Combine(dir, file.FileName)))
        using (var stream = file.OpenReadStream())
            await stream.CopyToAsync(filestream);

        await SoftList.TryAddNewPlugin(info, dir);
        return JsonApi.Success();
    }
}
