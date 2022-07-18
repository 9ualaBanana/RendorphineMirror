using Microsoft.AspNetCore.Mvc;
using ReepoBot.Services.Tasks;
using ReepoBot.Services.Telegram;

namespace ReepoBot.Controllers;

[Route("tasks")]
[ApiController]
public class TasksController : ControllerBase
{
    [HttpPost("result_preview")]
    public async Task NotifySubscribersAboutResultPreview(
    [FromQuery] string sessionId,
    [FromQuery] string iid,
    [FromServices] TelegramBot bot,
    [FromServices] TaskResultsPreviewer taskResultsPreviewer,
    [FromServices] ILogger<TasksController> logger)
    {
        logger.LogDebug("Received task result preview");

        var mpItem = await taskResultsPreviewer.GetMyMPItemAsync(sessionId, iid);
        if (mpItem.IsVideo)
        {
            var videoPreview = mpItem.AsVideoPreview;
            bot.TryNotifySubscribers(
                videoPreview.Mp4Url,
                logger,
                videoPreview.ThumbnailMediumUrl,
                caption: $"{videoPreview.Title}\n\nTask ID: **{videoPreview.TaskId}**\nM+ IID: **{videoPreview.MpIid}**",
                videoPreview.Width,
                videoPreview.Height);
        }
        else
            logger.LogError("Unsupported type for task result preview: {Type}", mpItem.Type);
    }
}
