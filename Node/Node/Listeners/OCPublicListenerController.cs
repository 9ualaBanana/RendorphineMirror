using System.Text;
using Microsoft.AspNetCore.Mvc;
using Node.Tasks.Watching.Handlers.Input;

namespace Node.Listeners;

[ApiController]
[Route("oc")]
public class OCPublicListenerController : ControllerBase
{
    readonly IWatchingTasksStorage WatchingTasks;
    readonly WatchingTasksHandler WatchingTasksHandler;

    public OCPublicListenerController(IWatchingTasksStorage watchingTasks, WatchingTasksHandler watchingTasksHandler)
    {
        WatchingTasks = watchingTasks;
        WatchingTasksHandler = watchingTasksHandler;
    }

    (OneClickWatchingTaskInputHandler handler, OneClickRunnerInfo runner) GetRunner()
    {
        var task = WatchingTasks.WatchingTasks.Values.First(d => d.Source is OneClickWatchingTaskInputInfo);
        var handler = WatchingTasksHandler.GetHandler<OneClickWatchingTaskInputHandler>(task);
        var runner = new OneClickRunnerInfo(handler.Input);

        return (handler, runner);
    }

    [HttpGet("getproducts")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetProducts()
    {
        var (handler, runner) = GetRunner();

        return Ok(JsonApi.JsonFromOpResultInline(new
        {
            products = runner.GetExportInfosByArchiveFiles(Directory.GetFiles(handler.Input.InputDirectory))
                .SelectMany(info => info.Unity?.Values.Select(u => u.ProductInfo) ?? Enumerable.Empty<ProductJson>())
                .WhereNotNull()
                .ToArray(),
        }.AsOpResult()));
    }
    [HttpGet("getexportstatus")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetExportStatus()
    {
        var (handler, runner) = GetRunner();

        return Ok(JsonApi.JsonFromOpResultInline(new
        {
            archives = Directory.GetFiles(runner.InputDir).Select(Path.GetFileName).ToArray(),
            export = runner.GetExportInfosByArchiveFilesDict(Directory.GetFiles(handler.Input.InputDirectory)),
        }.AsOpResult()));
    }


    void GetLogs(string archiveFileName, ProjectExportInfo exportInfo, out List<string> maxlogs, out List<string> unitylogs, out List<string> unitylogs2)
    {
        maxlogs = [];
        unitylogs = [];
        unitylogs2 = [];
        var (handler, runner) = GetRunner();

        try { maxlogs.AddRange(Directory.GetFiles(handler.Input.LogDirectory).Where(f => f.ContainsOrdinal(Path.GetFileNameWithoutExtension(archiveFileName)))); }
        catch { }
        try { unitylogs.AddRange(Directory.GetFiles(Path.Combine(handler.Input.LogDirectory, "unity")).Where(f => f.ContainsOrdinal(Path.GetFileNameWithoutExtension(archiveFileName)))); }
        catch { }

        try
        {
            foreach (var dir in Directory.GetDirectories(Path.Combine(handler.Input.OutputDirectory)))
            {
                var ddir = Path.Combine(dir, "unity", "Assets", exportInfo.ProductName, exportInfo.ProductName + ".log");
                if (System.IO.File.Exists(ddir))
                    unitylogs2.Add(ddir);
            }
        }
        catch { }
    }

    [HttpGet("getexportlogs")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetExportLogs([FromQuery] string archive)
    {
        var (handler, runner) = GetRunner();

        var exportinfo = runner.GetExportInfosByArchiveFiles([archive]);
        GetLogs(archive, exportinfo.First(), out var maxlogs, out var unitylogs, out var unitylogs2);

        return Ok(JsonConvert.SerializeObject(new { maxlogs, unitylogs, unitylogs2 }));
    }
    [HttpGet("getexportlog")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetExportLog([FromQuery] string archive, [FromQuery] string type, [FromQuery] int file)
    {
        var (handler, runner) = GetRunner();

        var fileidx = file;
        var exportinfo = runner.GetExportInfosByArchiveFiles([archive]);
        GetLogs(archive, exportinfo.First(), out var maxlogs, out var unitylogs, out var unitylogs2);

        var items = type switch
        {
            "max" => maxlogs,
            "unity" => unitylogs,
            "unity2" => unitylogs2,
            _ => throw new Exception("Unknown type")
        };
        var item = items[fileidx];

        var filename = Encoding.UTF8.GetBytes(item + "\n");

        using var filestream = System.IO.File.OpenRead(item);
        Response.ContentLength = filestream.Length + filename.Length;

        await Response.Body.WriteAsync(filename);
        await filestream.CopyToAsync(Response.Body);

        return Ok();
    }

    (List<string> maxlogs, List<string> unitylogs, List<string> unitylogs2) GetAllLogs()
    {
        var source = WatchingTasks.WatchingTasks.Values
            .Select(d => d.Source)
            .OfType<OneClickWatchingTaskInputInfo>()
            .FirstOrDefault();
        if (source is null) throw new Exception("NULL SOURCE");

        var maxlogs = new List<string>();
        var unitylogs = new List<string>();
        var unitylogs2 = new List<string>();
        try { maxlogs.AddRange(Directory.GetFiles(source.LogDirectory)); }
        catch { }
        try { unitylogs.AddRange(Directory.GetFiles(Path.Combine(source.LogDirectory, "unity"))); }
        catch { }

        try
        {
            foreach (var dir in Directory.GetDirectories(Path.Combine(source.OutputDirectory)))
            {
                foreach (var ddir in Directory.GetDirectories(Path.Combine(dir, "unity", "Assets")))
                {
                    var productName = Path.GetFileName(ddir);
                    if (System.IO.File.Exists(Path.Combine(ddir, productName + ".log")))
                        unitylogs2.Add(Path.Combine(ddir, productName + ".log"));
                }
            }
        }
        catch { }

        return (maxlogs, unitylogs, unitylogs2);
    }

    [HttpGet("getoclogs")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetOcLogs()
    {
        var (maxlogs, unitylogs, unitylogs2) = GetAllLogs();
        return Ok(JsonApi.JsonFromOpResultInline(new { maxlogs, unitylogs, unitylogs2 }.AsOpResult()));
    }

    [HttpGet("getoclog")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetOcLogs([FromQuery] int file, [FromQuery] string type)
    {
        var (maxlogs, unitylogs, unitylogs2) = GetAllLogs();
        var items = type switch
        {
            "max" => maxlogs,
            "unity" => unitylogs,
            "unity2" => unitylogs2,
            _ => throw new Exception("Unknown type")
        };
        var item = items[file];

        var filename = Encoding.UTF8.GetBytes(item + "\n");

        using var filestream = System.IO.File.OpenRead(item);
        Response.ContentLength = filestream.Length + filename.Length;

        await Response.Body.WriteAsync(filename);
        await filestream.CopyToAsync(Response.Body);

        return Ok();
    }
    static JObject RFProductToJson(RFProduct product)
    {
        return new JObject()
        {
            ["id"] = product.ID,
            ["type"] = product.Type,
            ["subproducts"] = new JArray(product.SubProducts.Select(RFProductToJson).ToArray()),
        };
    }

    [HttpGet("getocproducts")]
    public async Task<ActionResult> GetOcProducts([FromServices] IRFProductStorage rfProducts)
    {
        Response.Headers.AccessControlAllowCredentials = "*";
        return Ok(JsonApi.Success(rfProducts.RFProducts.Select(p => KeyValuePair.Create(p.Key, RFProductToJson(p.Value))).ToImmutableDictionary()));
    }
    [HttpGet("getocproductdata")]
    public async Task<ActionResult> GetOcProducts([FromQuery] string id, [FromServices] IRFProductStorage rfProducts)
    {
        if (!rfProducts.RFProducts.TryGetValue(id, out var rfp))
            return NotFound();

        return Ok(JsonApi.Success(RFProductToJson(rfp)));
    }
    [HttpGet("getocproductqsp")]
    public async Task<ActionResult> GetOcProducts([FromQuery] string id, [FromQuery] string type, [FromServices] IRFProductStorage rfProducts)
    {
        if (!rfProducts.RFProducts.TryGetValue(id, out var product))
            return Ok(JsonApi.Error("Unknown product"));

        if (type is not ("imagefooter" or "imageqr" or "video"))
            return Ok(JsonApi.Error("Unknown type"));

        var filepath = JObject.FromObject(product.QSPreview).Property(type, StringComparison.OrdinalIgnoreCase)
            .ThrowIfNull("Type is not present on file").Value.ToObject<FileWithFormat>().ThrowIfNull().Path;

        return PhysicalFile(filepath, MimeTypes.GetMimeType(filepath));
    }

    [HttpGet("getocproductfile")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetOcProductFile([FromQuery] string id, [FromServices] IRFProductStorage rfProducts)
    {
        if (!rfProducts.RFProducts.TryGetValue(id, out var product))
            return Ok(JsonApi.Error("Unknown product"));

        var filepath = product.Idea.Path;
        return PhysicalFile(filepath, MimeTypes.GetMimeType(filepath));
    }
    [HttpGet("getocproductdir")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetOcProductDir([FromQuery] string id, [FromServices] IRFProductStorage rfProducts)
    {
        if (!rfProducts.RFProducts.TryGetValue(id, out var product))
            return Ok(JsonApi.Error("Unknown product"));

        var dirpath = Path.GetFullPath(product.Idea.Path);
        var result = new JArray(Directory.GetFiles(dirpath, "*", SearchOption.AllDirectories).Select(p => Path.GetRelativePath(dirpath, p)).ToArray());

        return Ok(JsonApi.Success(result));
    }
    [HttpGet("getocproductdirfile")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetOcProductDirFile([FromQuery] string id, [FromQuery] string file, [FromServices] IRFProductStorage rfProducts)
    {
        if (!rfProducts.RFProducts.TryGetValue(id, out var product))
            return Ok(JsonApi.Error("Unknown product"));

        var filepath = Path.GetFullPath(Path.Combine(product.Idea.Path, file));
        if (!filepath.StartsWith(Path.GetFullPath(product.Idea.Path), StringComparison.Ordinal))
            return NotFound(); ;

        return PhysicalFile(filepath, MimeTypes.GetMimeType(filepath));
    }
    [HttpGet("getmynodesids")]
    [SessionIdAuthorization]
    public async Task<ActionResult> GetMyNodesIps([FromServices] Apis api)
    {
        var result = await api.GetMyNodesAsync()
            .Next(nodes => nodes.Select(n => $"{n.Info.Ip}:{n.Info.Port}").ToArray().AsOpResult());

        return Ok(JsonApi.Success(result));
    }

    [HttpPost("loginas")]
    [SessionIdAuthorization]
    public async Task<ActionResult> LoginAs([FromForm] string email, [FromForm] string password, [FromServices] Apis api)
    {
        var result = await api.Api.ApiPost<SessionManager.LoginResult>($"{(global::Common.Api.TaskManagerEndpoint)}/login", null, "Logging in", ("email", email), ("password", password), ("lifetime", TimeSpan.FromDays(1).TotalMilliseconds.ToString()), ("guid", Guid.NewGuid().ToString()));

        // sessionid of another account
        if (result.Success && result.Value.UserId != Settings.UserId)
        {
            var nodes = await api.WithSessionId(result.Value.SessionId).GetMyNodesAsync();
            if (!nodes)
                return Ok(JsonApi.Error("Unknown server error"));

            if (nodes.Value.Length == 0)
                return Ok(JsonApi.Error("Account has no nodes"));

            var node = null as NodeInfo;
            foreach (var n in nodes.Value)
            {
                try
                {
                    var online = (await api.Api.Client.GetAsync($"http://{n.Ip}:{n.Info.Port}/ping", new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token)).IsSuccessStatusCode;
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

        return Ok(JsonApi.Success(result));
    }
    [HttpPost("loginassid")]
    [SessionIdAuthorization]
    public async Task<ActionResult> LoginAsSessionId([FromForm] string sid, [FromServices] Apis api) => Ok(JsonApi.Success(sid));
}
