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
}
