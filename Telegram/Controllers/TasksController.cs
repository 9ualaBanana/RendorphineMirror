﻿using Common;
using Common.Tasks;
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
    [FromServices] ILogger<TasksController> logger,
    [FromServices] IHttpClientFactory httpClientFactory)
    {
        logger.LogDebug("Received task result preview");

        var mpItem = await taskResultsPreviewer.GetMyMPItemAsync(taskId, nodeName);

        if (taskRegistry.Remove(taskId, out var authenticationToken))
        {
            await httpClientFactory.CreateClient()
                .GetAsync($"{Api.TaskManagerEndpoint}/mytaskstatechanged?sessionid={authenticationToken.MPlus.SessionId}&taskid={taskId}&newstate={TaskState.Finished.ToString().ToLowerInvariant()}");

            if (mpItem is not null) await mpItem.SendWith(bot, authenticationToken.ChatId);
            else await bot.TrySendMessageAsync(authenticationToken.ChatId, $"Couldn't retrieve the resulting M+ item for ({taskId}).");
            
        }

        return JsonContent.Create(new { ok = 1 });
    }

    [HttpGet("getinput/{id}")]
    public ActionResult GetInput([FromRoute] string id, [FromServices] TelegramFileRegistry fileRegistry)
    {
        if (fileRegistry.TryGet(id) is null) return NotFound();

        var fileName = Path.ChangeExtension(Path.Combine(_appEnvironment.ContentRootPath, fileRegistry.Path, id), ".jpg");
        try { return PhysicalFile(fileName, MimeTypes.GetMimeType(fileName)); }
        catch { return NotFound(); }
    }
}
