using Microsoft.AspNetCore.Mvc;
using Telegram.Telegram.FileRegistry;

namespace Telegram.Telegram.Updates.Tasks.Controllers;

[ApiController]
[Route("tasks")]
public class TasksController : ControllerBase
{
    [HttpPost("result_preview")]
    public async Task NotifySubscribersAboutResultPreview(
        [FromQuery] string taskId,
        [FromQuery] string shardHost,
        [FromQuery] string[] iids,
        [FromQuery] string taskExecutor,
        [FromServices] TaskResultPreviewService taskResultPreviewService,
        CancellationToken cancellationToken)
    {
        var taskApi = new ApiTask(taskId, iids) { HostShard = shardHost };
        await taskResultPreviewService.SendTaskResultPreviewsAsyncUsing(taskApi, iids, taskExecutor, cancellationToken);
    }

    [HttpGet("getinput/{id}")]
    public ActionResult GetInput([FromRoute] string id, [FromServices] TelegramFileRegistry fileRegistry, [FromServices] IWebHostEnvironment environment)
    {
        var file = fileRegistry.TryGet(id);
        if (file is null) return NotFound();

        var fileName = Path.ChangeExtension(Path.Combine(environment.ContentRootPath, fileRegistry.Path, id), file.Extension);

        try { return PhysicalFile(fileName, MimeTypes.GetMimeType(file.Extension)); }
        catch { return NotFound(); }
    }
}
