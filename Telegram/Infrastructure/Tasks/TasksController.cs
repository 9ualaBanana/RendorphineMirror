using Microsoft.AspNetCore.Mvc;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Tasks.ResultPreview;

namespace Telegram.Infrastructure.Tasks;

[ApiController]
[Route("tasks")]
public class TasksController : ControllerBase
{
    [HttpPost("result")]
    public async Task Handle(
        [FromQuery] ExecutedTaskApi executedTaskApi,
        [FromServices] TelegramPreviewTaskResultHandler taskResultHandler)
        => await taskResultHandler.SendPreviewAsyncUsing(executedTaskApi, HttpContext.RequestAborted);

    [HttpGet("getinput/{index}")]
    public ActionResult GetInput([FromRoute] Guid index, [FromServices] MediaFilesCache mediaFilesCache)
    {
        if (mediaFilesCache.TryRetrieveMediaFileWith(index) is CachedMediaFile cachedTaskInputFile)
            return PhysicalFile(cachedTaskInputFile.Path, cachedTaskInputFile.File.MimeType);
        else return NotFound();
    }
}
