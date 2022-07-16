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
        var videoPreview = mpItem.GetProperty("videopreview").GetProperty("mp4.url").GetString()!;
        var thumbnail = mpItem.GetProperty("previewurl").GetString()!;

        bot.TryNotifySubscribers(videoPreview, thumbnail, logger);
    }
}
