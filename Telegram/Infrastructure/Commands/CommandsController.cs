using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Telegram.Security.Authorization;
using Telegram.Commands.Handlers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Telegram.Infrastructure.Commands;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class CommandsController : ControllerBase
{
    internal const string PathFragment = "command";

    /// <remarks>
    /// Explicitly requested via DI because <see cref="Command"/>s have requirements
    /// that must be explicitly passed to <see cref="IAuthorizationService"/> during imperative authorization.
    /// </remarks>
    readonly IAuthorizationService _authorizationService;

    readonly ILogger<CommandsController> _logger;

    public CommandsController(IAuthorizationService authorizationService, ILogger<CommandsController> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task Handle([FromServices] IEnumerable<CommandHandler> commandHandlers)
    {
        string rawCommand = HttpContext.GetUpdate().Message!.Text!;

        if (commandHandlers.Switch(rawCommand) is CommandHandler command)
        {
            var authorizationResult = await UserIsAuthorizedToCall(command);
            if (authorizationResult.Succeeded)
                await command.HandleAsync();
            else
            {
                _logger.LogTrace("User is not authorized to use {Command} command", rawCommand);
                if (authorizationResult.Failure!.FailedRequirements.Any(requirement => requirement is DenyAnonymousAuthorizationRequirement))
                    await HttpContext.ChallengeAsync();
            }
        }
        else _logger.LogTrace("{Command} command is unknown", rawCommand);
    }

    async Task<AuthorizationResult> UserIsAuthorizedToCall(CommandHandler command)
    {
        if (command is IAuthorizationPolicyProtected command_)
            return await _authorizationService.AuthorizeAsync(User, command_, command_.AuthorizationPolicy);
        else return AuthorizationResult.Success();
    }
}
