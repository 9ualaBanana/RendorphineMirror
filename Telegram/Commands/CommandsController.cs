using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Telegram.Models;
using Telegram.Security.Authorization;
using Telegram.Commands.Handlers;

namespace Telegram.Commands;

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

        if (commandHandlers.Switch(rawCommand) is CommandHandler commandHandler)
        {
            if (await UserIsAuthorizedToCall(commandHandler))
                await commandHandler.HandleAsync(HttpContext);
            else
            {
                _logger.LogTrace("User is not authorized to use {Command} command", rawCommand);
                await HttpContext.ChallengeAsync();
            }
        }
        else _logger.LogTrace("{Command} command is unknown", rawCommand);
    }

    async Task<bool> UserIsAuthorizedToCall(CommandHandler command)
    {
        bool isAuthorized = true;

        if (command is IAuthorizationRequirementsProvider command_ && command_.Requirements.Any())
        {
            var authorizationResult = await _authorizationService
                .AuthorizeAsync(User, command_, command_.Requirements);
            isAuthorized = authorizationResult.Succeeded;
        }

        return isAuthorized;
    }
}
