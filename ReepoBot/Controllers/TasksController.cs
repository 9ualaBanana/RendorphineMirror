using Microsoft.AspNetCore.Mvc;
using ReepoBot.Services.Tasks;
using ReepoBot.Services.Telegram;
using ReepoBot.Services.Telegram.FileRegistry;

namespace ReepoBot.Controllers;

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
    [FromQuery] string sessionId,
    [FromQuery] string taskId,
    [FromQuery] string nodeName,
    [FromServices] TelegramBot bot,
    [FromServices] TaskResultsPreviewer taskResultsPreviewer,
    [FromServices] ILogger<TasksController> logger)
    {
        logger.LogDebug("Received task result preview");

        var mpItem = await taskResultsPreviewer.GetMyMPItemAsync(sessionId, taskId);
        if (mpItem.IsVideo)
        {
            var videoPreview = mpItem.AsVideoPreview;
            await bot.TryNotifySubscribersAboutVideoAsync(
                videoPreview.Mp4Url,
                videoPreview.ThumbnailMediumUrl,
                caption: $"{videoPreview.Title}\n\nNode: {nodeName}\nTask ID: *{videoPreview.TaskId}*\nM+ IID: *{videoPreview.MpIid}*",
                videoPreview.Width,
                videoPreview.Height);
        }
        else if (mpItem.IsImage)
        {
            var imagePreview = mpItem.AsImagePreview;
            await bot.TryNotifySubscribersAboutImageAsync(
            imagePreview.ThumbnailMediumUrl,
            caption: $"{imagePreview.Title}\n\nNode: {nodeName}\nTask ID: *{imagePreview.TaskId}*\nM+ IID: *{imagePreview.MpIid}*");
        }
        else
            logger.LogError("Unsupported type for task result preview: {Type}", mpItem.Type);
        return JsonContent.Create(new { ok = 1 });
    }

    [HttpGet("getinput/{id}")]
    public FileResult GetInput([FromRoute] string id, [FromServices] TelegramFileRegistry fileRegistry)
    {
        if (fileRegistry.TryGet(id) is null) NotFound();
        return PhysicalFile(Path.Combine(_appEnvironment.ContentRootPath, fileRegistry.Path, id), "image/jpg");
    }
}
