using Microsoft.AspNetCore.Mvc;
using ReepoBot.Controllers.Binders;
using ReepoBot.Services;
using ReepoBot.Services.GitHub;
using ReepoBot.Services.Hardware;
using ReepoBot.Services.Telegram;
using System.Text.Json;
using Telegram.Bot.Types;

namespace ReepoBot.Controllers;

[ApiController]
[Route("")]
public class WebhookEventReceiversController : ControllerBase
{
    // Telegram.Bot works only with Newtonsoft.
    [HttpPost("telegram")]
    public async Task<ActionResult> ReceiveUpdate(
        [NewtonsoftJsonBinder] Update update,
        [FromServices] WebhookEventHandlerFactory<TelegramUpdateHandler, Update> handlerFactory)
    {
        var updateHandler = handlerFactory.Resolve(update);
        if (updateHandler is null) return Ok();

        await updateHandler.HandleAsync(update);
        // Telegram doesn't seem to like other status codes so ok is sent in any case.
        return Ok();
    }

    [HttpPost("github")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ReceiveGitHubEvent(
        [FromHeader(Name = "X-Hub-Signature-256")] string signature,
        [FromHeader(Name = "X-GitHub-Event")] string eventType,
        [FromServices] WebhookEventHandlerFactory<GitHubWebhookEventForwarder, string> handlerFactory,
        [FromServices] IConfiguration configuration,
        [FromBody] JsonDocument payload)
    {
        var eventHandler = handlerFactory.Resolve(eventType);
        if (eventHandler is null)
        {
            return BadRequest();
        }

        var payloadContent = payload.RootElement;
        if (!eventHandler.SignaturesMatch(payloadContent, signature, configuration["GitHubSecret"]))
        {
            return BadRequest();
        }

        await eventHandler.HandleAsync(payloadContent);
        return Ok();
    }

    [HttpPost("hardware_info")]
    public async Task ReceiveHardwareInformation(
        JsonElement hardwareInfoMessageJson,
        [FromServices] HardwareInfoForwarder hardwareInfoForwarder,
        [FromServices] ILogger<WebhookEventReceiversController> logger
        )
    {
        try
        {
            logger.LogDebug("JSON with the hardware info message is received.");
            var hardwareInfoMessage = hardwareInfoMessageJson.GetProperty("message").GetString();
            logger.LogDebug("Hardware message is retrieved from JSON.");
            await hardwareInfoForwarder.HandleAsync(hardwareInfoMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{receiver} couldn't handle JSON hardware info message with the following content:\n{message}",
                nameof(ReceiveHardwareInformation), hardwareInfoMessageJson);
        }
    }
}
