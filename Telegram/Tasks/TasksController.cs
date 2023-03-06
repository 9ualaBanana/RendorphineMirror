using Microsoft.AspNetCore.Mvc;
using Telegram.Models;
using Telegram.Telegram.FileRegistry;

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
    public ActionResult GetInput([FromRoute] string id, [FromServices] CachedMediaFiles cachedMediaFiles)
    {
        if (cachedMediaFiles[id] is MediaFile mediaFile)
        {
            var mediaFileName = Path.ChangeExtension(Path.Combine(cachedMediaFiles.PathFor(mediaFile, id)), mediaFile.Extension);
            return PhysicalFile(mediaFileName, MimeTypes.GetMimeType(mediaFile.Extension));
        }
        else return NotFound();
    }
}
