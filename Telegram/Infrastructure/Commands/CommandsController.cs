using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Telegram.Security.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Telegram.Infrastructure.Commands;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class CommandsController : ControllerBase
{
    internal const string PathFragment = "command";

    /// <remarks>
    /// Explicitly requested via DI because some <see cref="Command"/>s are <see cref="IAuthorizationPolicyProtected"/> and
    /// their <see cref="AuthorizationPolicy"/> must be explicitly passed to <see cref="IAuthorizationService"/> during imperative authorization.
    /// </remarks>
    readonly IAuthorizationService _authorizationService;
    readonly Command.Received _receivedCommand;

    readonly ILogger<CommandsController> _logger;

    public CommandsController(
        IAuthorizationService authorizationService,
        Command.Received receivedCommand,
        ILogger<CommandsController> logger)
    {
        _authorizationService = authorizationService;
        _receivedCommand = receivedCommand;
        _logger = logger;
    }

    [HttpPost]
    public async Task Handle([FromServices] IEnumerable<CommandHandler> commandHandlers)
    {
        var receivedCommand = _receivedCommand.Get();

        if (commandHandlers.Switch(receivedCommand) is CommandHandler command)
        {
            var authorizationResult = await UserIsAuthorizedToCall(command);
            if (authorizationResult.Succeeded)
                await command.HandleAsync();
            else
            {
                _logger.LogTrace("User is not authorized to use {Command} command", receivedCommand);
                if (authorizationResult.Failure!.FailedRequirements.Any(requirement => requirement is DenyAnonymousAuthorizationRequirement))
                    await HttpContext.ChallengeAsync();
            }
        }
        else _logger.LogTrace("{Command} command is unknown", receivedCommand);


        async Task<AuthorizationResult> UserIsAuthorizedToCall(CommandHandler command)
        {
            if (command is IAuthorizationPolicyProtected command_)
                return await _authorizationService.AuthorizeAsync(User, command_, command_.AuthorizationPolicy);
            else return AuthorizationResult.Success();
        }
    }
}
