using Microsoft.AspNetCore.Mvc;
using Telegram.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Images.Models;

namespace Telegram.Tasks;

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
        [FromServices] TaskResultMPlusPreviewService taskResultPreviewService,
        CancellationToken cancellationToken)
    {
        var taskApi = new ApiTask(taskId, iids) { HostShard = shardHost };
        await taskResultPreviewService.SendTaskResultPreviewsAsyncUsing(taskApi, iids, taskExecutor, cancellationToken);
    }

    [HttpGet("getinput/{id}")]
    public ActionResult GetInput([FromRoute] string id, [FromServices] CachedFiles cachedFiles, [FromServices] IWebHostEnvironment environment)
    {
        if (cachedFiles[id] is TelegramMediaFile file)
        {
            var fileName = Path.ChangeExtension(Path.Combine(environment.ContentRootPath, cachedFiles.Location, id), file.Extension);

            try { return PhysicalFile(fileName, MimeTypes.GetMimeType(file.Extension)); }
            catch { }
        }
        
        return NotFound();
    }
}
