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

    [HttpGet("getinput/{id}")]
    public ActionResult GetInput([FromRoute] string id, [FromServices] MediaFilesCache mediaFilesCache)
    {
        if (mediaFilesCache[id] is MediaFile mediaFile)
        {
            var mediaFileName = Path.ChangeExtension(Path.Combine(mediaFilesCache.PathFor(mediaFile, id)), mediaFile.Extension);
            return PhysicalFile(mediaFileName, MimeTypes.GetMimeType(mediaFile.Extension));
        }
        else return NotFound();
    }
}
