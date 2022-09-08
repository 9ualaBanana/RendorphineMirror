using Microsoft.AspNetCore.Mvc;
using Telegram.Services.Tasks;
using Telegram.Services.Telegram;
using Telegram.Services.Telegram.FileRegistry;

namespace Telegram.Controllers;

[Route("tasks")]
[ApiController]
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
    [FromQuery] string nodeName,
    [FromServices] TelegramBot bot,
    [FromServices] TaskResultsPreviewer taskResultsPreviewer,
    [FromServices] TaskRegistry taskRegistry,
    [FromServices] ILogger<TasksController> logger)
    {
        logger.LogDebug("Received task result preview");

        var mpItem = await taskResultsPreviewer.GetMyMPItemAsync(taskId);

        if (taskRegistry.Remove(taskId, out var authenticationToken))
        {
            if (mpItem is null)
                await bot.TryNotifySubscribersAsync($"Couldn't retrieve the task result ({taskId}).");
            else if (mpItem.IsVideo)
            {
                var videoPreview = mpItem.AsVideoPreview;
                await bot.TrySendVideoAsync(
                    authenticationToken.ChatId,
                    videoPreview.Mp4Url,
                    videoPreview.ThumbnailMediumUrl,
                    caption: $"{videoPreview.Title}\n\nNode: {nodeName}\nTask ID: *{videoPreview.TaskId}*\nM+ IID: *{videoPreview.MpIid}*",
                    videoPreview.Width,
                    videoPreview.Height);
            }
            else if (mpItem.IsImage)
            {
                var imagePreview = mpItem.AsImagePreview;
                await bot.TrySendImageAsync(
                    authenticationToken.ChatId,
                    imagePreview.ThumbnailMediumUrl,
                    caption: $"{imagePreview.Title}\n\nNode: {nodeName}\nTask ID: *{imagePreview.TaskId}*\nM+ IID: *{imagePreview.MpIid}*");
            }
            else
                logger.LogError("Unsupported type for task result preview: {Type}", mpItem.Type);
        }

        return JsonContent.Create(new { ok = 1 });
    }

    [HttpGet("getinput/{id}")]
    public ActionResult GetInput([FromRoute] string id, [FromServices] TelegramFileRegistry fileRegistry)
    {
        if (fileRegistry.TryGet(id) is null) return NotFound();
        try { return PhysicalFile(Path.Combine(_appEnvironment.ContentRootPath, fileRegistry.Path, id), "image/jpg"); }
        catch { return NotFound(); }
    }
}
