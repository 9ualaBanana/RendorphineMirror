using Microsoft.AspNetCore.Mvc;
using ReepoBot.Models;
using ReepoBot.Services.GitHub;
using System.Text.Json;

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
        [FromServices] IConfiguration configuration,
        [FromBody] JsonDocument payload)
    {
        var gitHubEvent = new GitHubEvent(eventType, signature, payload.RootElement);
        if (!gitHubEventForwarder.SignaturesMatch(gitHubEvent, configuration["GitHubSecret"]))
        {
            return BadRequest();
        }
        await gitHubEventForwarder.HandleAsync(gitHubEvent);
        return Ok();
    }
}
