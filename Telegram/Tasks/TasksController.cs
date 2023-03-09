using Microsoft.AspNetCore.Mvc;
using Telegram.MediaFiles;
using Telegram.Tasks.ResultPreview;

namespace Telegram.Tasks;

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
    public ActionResult GetInput([FromRoute] string index, [FromServices] MediaFilesCache mediaFilesCache)
    {
        if (mediaFilesCache.RetrieveMediaFileWith(index) is CachedMediaFile cachedTaskInputFile)
            return PhysicalFile(cachedTaskInputFile.Path, MimeTypes.GetMimeType(cachedTaskInputFile.File.Extension.ToString()));
        else return NotFound();
    }
}
