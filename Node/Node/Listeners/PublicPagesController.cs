using Microsoft.AspNetCore.Mvc;

namespace Node.Listeners;

[ApiController]
public class PublicPagesController : ControllerBase
{
    readonly Apis Api;

    public PublicPagesController(Apis api) => Api = api;

    [HttpGet("logs")]
    [SessionIdAuthorization]
    public async Task<ActionResult> Logs([FromQuery] string? id)
    {
        var logDir = Path.GetFullPath("logs");

        if (id is null)
        {
            var info = $"""
                <!DOCTYPE html>
                <html>
                <head>
                    <title>Log {id}</title>
                </head>
                <body>
                """;

            addFolder(logDir);


            void addFolder(string folder)
            {
                var files = Directory.GetFiles(folder);
                info += $"<b style='font-size: 32px'>{Path.GetFileName(folder)}</b></br>";
                foreach (var file in files)
                    info += $"<a href='/logs?id={Path.GetRelativePath(logDir, file)}'>{Path.GetFileName(file)}</a></br>";

                info += "<div style=\"margin-left:4px\">";
                foreach (var f in Directory.GetDirectories(folder))
                    addFolder(f);

                info += "</div>";
            }

            info += """
                </body>
                </html>
                """;

            return Content(info, "text/html");
        }

        var filepath = Path.Combine(logDir, id);
        if (!Path.GetFullPath(filepath).StartsWith(logDir, StringComparison.Ordinal))
            return NotFound();

        return File(filepath, "text/plain");
    }

    [HttpPost("loginas")]
    public async Task<ActionResult> LoginAs([FromForm] string email, [FromForm] string password)
    {
        var result = await Api.Api.ApiPost<SessionManager.LoginResult>($"{(global::Common.Api.TaskManagerEndpoint)}/login", null, "Logging in", ("email", email), ("password", password), ("lifetime", TimeSpan.FromDays(1).TotalMilliseconds.ToString()), ("guid", Guid.NewGuid().ToString()));

        // sessionid of another account
        if (!result.Success | result.Value.UserId == Settings.UserId)
            return Ok(JsonApi.JsonFromOpResult(result));

        var nodes = await Api.WithSessionId(result.Value.SessionId).GetMyNodesAsync();
        if (!nodes)
            return Ok(JsonApi.Error("Unknown server error"));

        if (nodes.Value.Length == 0)
            return Ok(JsonApi.Error("Account has no nodes"));

        var node = null as NodeInfo;
        foreach (var n in nodes.Value)
        {
            try
            {
                var online = (await Api.Api.Client.GetAsync($"http://{n.Ip}:{n.Info.Port}/ping", new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token)).IsSuccessStatusCode;
                if (!online) continue;

                node = n;
                break;
            }
            catch { }
        }
        if (node is null)
            return Ok(JsonApi.Error("No online nodes found on that account"));

        return Redirect($"http://{node.Ip}:{node.Info.WebPort}/loginas");
    }

    [HttpGet("getcontents")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetContents([FromQuery] string? path = null)
    {
        DirectoryContents contents;

        if (path is (null or ""))
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                contents = new DirectoryContents("", DriveInfo.GetDrives().Select(x => x.RootDirectory.FullName).ToImmutableArray());
            else contents = new DirectoryContents("/", Directory.GetDirectories("/").Select(x => Path.GetRelativePath("/", x)).ToImmutableArray());
        }
        else
        {
            if (!Directory.Exists(path))
                return Ok(JsonApi.Error("Directory does not exists"));

            contents = new DirectoryContents(path, Directory.GetDirectories(path).Select(x => Path.GetRelativePath(path, x)).ToImmutableArray());
        }

        return Ok(JsonApi.Success(contents));
    }

    [HttpGet("getfile")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetFile([FromQuery] string path)
    {
        if (!System.IO.File.Exists(path))
            return Ok(JsonApi.Error("File does not exists"));

        var temp = Path.GetTempFileName();
        System.IO.File.Copy(path, temp, true);

        return new DeletedPhysicalFileResult(temp, MimeTypes.GetMimeType(path)) { FileName = Path.GetFileName(path) };
    }

    [HttpGet("stats/getloadbetween")]
    public async Task<ActionResult> GetLoadBetween([FromQuery] long start, [FromQuery] long end, [FromQuery] long stephours, [FromServices] SystemLoadStoreService loadService) =>
        Ok(JsonApi.Success(await loadService.Load(start, end, stephours)));


    class DeletedPhysicalFileResult : PhysicalFileResult
    {
        public DeletedPhysicalFileResult(string fileName, string contentType) : base(fileName, contentType) { }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            try { await base.ExecuteResultAsync(context); }
            finally { System.IO.File.Delete(FileName); }
        }
    }
}
