using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ReepoBot.Models;
using ReepoBot.Services.GitHub;

namespace ReepoBot.Controllers;

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
        [FromServices] IConfiguration configuration,
        [FromBody] JObject payload)
    {
        logger.LogDebug("GitHub event with {Type} is received", eventType);
        var gitHubEvent = new GitHubEvent(eventType, signature, payload.Root);
        if (!gitHubEventForwarder.SignaturesMatch(gitHubEvent, configuration["GitHubSecret"]))
        {
            return BadRequest();
        }
        try
        {
            await gitHubEventForwarder.HandleAsync(gitHubEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex, @"Something went wrong when trying to handle {Type} GitHub event.\nPayload:\n{Payload}",
                eventType, payload.ToString());
        }
        return Ok();
    }
}
