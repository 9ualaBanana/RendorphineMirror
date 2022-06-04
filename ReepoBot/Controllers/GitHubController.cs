using Microsoft.AspNetCore.Mvc;
using ReepoBot.Models;
using ReepoBot.Services.GitHub;
using System.Text.Json;

namespace ReepoBot.Controllers;

[ApiController]
[Route("")]
public class GitHubController : ControllerBase
{
    [HttpPost("github")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult> ReceiveGitHubEvent(
        [FromHeader(Name = "X-Hub-Signature-256")] string signature,
        [FromHeader(Name = "X-GitHub-Event")] string eventType,
        [FromServices] GitHubWebhookEventForwarder gitHubWebhookEventForwarder,
        [FromServices] IConfiguration configuration,
        [FromBody] JsonDocument payload)
    {
        var gitHubEvent = new GitHubWebhookEvent(eventType, signature, payload.RootElement);
        if (!gitHubWebhookEventForwarder.SignaturesMatch(gitHubEvent, configuration["GitHubSecret"]))
        {
            return BadRequest();
        }
        await gitHubWebhookEventForwarder.HandleAsync(gitHubEvent);
        return Ok();
    }
}
