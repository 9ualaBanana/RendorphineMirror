using Microsoft.AspNetCore.Mvc;
using Telegram.Models;
﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Telegram.Commands;
using Telegram.Security.Authorization;

namespace Telegram.Controllers;

[ApiController]
[Route($"telegram/{{token}}/{PathFragment}")]
public class CommandController : ControllerBase
{
    internal const string PathFragment = "command";

    /// <remarks>
    /// Explicitly requested via DI because <see cref="Command"/>s have requirements
    /// that must be explicitly passed to <see cref="IAuthorizationService"/> during imperative authorization.
    /// </remarks>
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<CommandController> _logger;

    public CommandController(IAuthorizationService authorizationService, ILogger<CommandController> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    [HttpPost]
    public async Task Handle([FromServices] IEnumerable<CommandHandler> commandHandlers)
    {
        string prefixedCommandText = HttpContext.GetUpdate().Message!.Text!;

        if (commandHandlers.Switch(prefixedCommandText) is CommandHandler commandHandler)
        {
            if (await UserIsAuthorizedToCall(commandHandler))
                await commandHandler.HandleAsync(HttpContext, HttpContext.RequestAborted);
            else
            {
                _logger.LogTrace("User is not authorized to use {Command} command", prefixedCommandText);
                await HttpContext.ChallengeAsync();
            }
        }
        else _logger.LogTrace("{Command} command is unknown", prefixedCommandText);
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
