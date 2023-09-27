using HeyRed.Mime;
using Microsoft.AspNetCore.Mvc;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Tasks;

namespace Telegram.Infrastructure.Tasks;

[ApiController]
[Route("tasks")]
public class RTasksController : ControllerBase
{
    [HttpPost("result")]
    public async Task Handle(
        [FromQuery] ExecutedRTaskApi executedRTaskApi,
        [FromServices] BotRTaskPreview botRTaskPreview)
        => await botRTaskPreview.SendAsyncUsing(executedRTaskApi, HttpContext.RequestAborted);

    [HttpGet("getinput/{index}")]
    public ActionResult GetInput([FromRoute] Guid index, [FromServices] MediaFilesCache mediaFilesCache)
    {
        if (mediaFilesCache.TryRetrieveMediaFileWith(index) is MediaFilesCache.Entry cachedTaskInputFile)
            return PhysicalFile(cachedTaskInputFile.File.FullName, MimeTypesMap.GetMimeType(cachedTaskInputFile.File.Extension));
        else return NotFound();
    }
}
