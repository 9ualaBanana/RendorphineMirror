using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Telegram.Models;
using Telegram.Services.GitHub;

namespace Telegram.Controllers;

[ApiController]
[Route("github")]
public class GitHubController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ReceiveGitHubEvent(
        [FromHeader(Name = "X-Hub-Signature-256")] string signature,
        [FromHeader(Name = "X-GitHub-Event")] string eventType,
        [FromServices] GitHubEventForwarder gitHubEventForwarder,
        [FromServices] ILogger<GitHubController> logger,
        [FromBody] JObject payload)
    {
        logger.LogDebug("GitHub event with {Type} type is received", eventType);
        var gitHubEvent = new GitHubEvent(eventType, signature, payload);
        try
        {
            await gitHubEventForwarder.HandleAsync(gitHubEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, "Something went wrong when trying to handle {Type} GitHub event.\nPayload:\n{Payload}",
                eventType, payload.ToString());
        }
        return Ok();
    }
}
