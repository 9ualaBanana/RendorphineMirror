using HeyRed.Mime;
using Microsoft.AspNetCore.Mvc;
using Telegram.Infrastructure.Media;
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
        if (mediaFilesCache.TryRetrieveMediaFileWith(index) is MediaFilesCache.Entry cachedTaskInputFile)
            return PhysicalFile(cachedTaskInputFile.File.FullName, MimeTypesMap.GetMimeType(cachedTaskInputFile.File.Extension));
        else return NotFound();
    }
}
