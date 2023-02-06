using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Tasks.ResultsPreview.Services;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates.Tasks.Controllers;

[ApiController]
[Route("tasks")]
public class TasksController : ControllerBase
{
    readonly IWebHostEnvironment _appEnvironment;


    public TasksController(IWebHostEnvironment appEnvironment)
    {
        _appEnvironment = appEnvironment;
    }


    [HttpPost("result_preview")]
    public async Task<JsonContent> NotifySubscribersAboutResultPreview(
    [FromQuery] string taskId,
    [FromQuery] string[] iids,
    [FromQuery] string shardHost,
    [FromQuery] string nodeName,
    [FromServices] TelegramBot bot,
    [FromServices] TaskResultsPreviewer taskResultsPreviewer,
    [FromServices] TaskResultPreviewService taskResultPreviewService,
    [FromServices] TaskRegistry taskRegistry,
    [FromServices] ILogger<TasksController> logger,
    CancellationToken cancellationToken)
    {
        logger.LogDebug("Received task result preview");
        var taskApi = new ApiTask(taskId, iids) { HostShard = shardHost };

        if (taskRegistry.Remove(taskId, out var authenticationToken))
        {
            foreach (var iid in iids)
            {
                var resultPreview = await taskResultPreviewService
                    .RequestTaskResultPreviewAsyncUsing(taskApi, authenticationToken.MPlus.SessionId, nodeName, iid, cancellationToken);
                
            }
            await Apis.Default.WithSessionId(authenticationToken.MPlus.SessionId).ChangeStateAsync(taskApi, TaskState.Finished).ThrowIfError();
        }

            //if (mpItem is not null) await mpItem.SendWith(bot, authenticationToken.ChatId);
            //else await bot.SendMessageAsync_(authenticationToken.ChatId, $"Couldn't retrieve the resulting M+ item for ({taskId}).");

        return JsonContent.Create(new { ok = 1 });
    }

    [HttpGet("getinput/{id}")]
    public ActionResult GetInput([FromRoute] string id, [FromServices] TelegramFileRegistry fileRegistry)
    {
        var file = fileRegistry.TryGet(id);
        if (file is null) return NotFound();

        var fileName = Path.ChangeExtension(Path.Combine(_appEnvironment.ContentRootPath, fileRegistry.Path, id), file.Extension);

        try { return PhysicalFile(fileName, MimeTypes.GetMimeType(file.Extension)); }
        catch { return NotFound(); }
    }
}
