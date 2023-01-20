using Microsoft.AspNetCore.Mvc;
using Telegram.Models;

namespace Telegram.Controllers;

[ApiController]
[Route($"telegram/{PathFragment}")]
public class CommandController : ControllerBase
{
    internal const string PathFragment = "command";

    public async Task Handle(
        [FromServices] IEnumerable<Command> commands,
        [FromServices] UpdateContextCache updateContextCache,
        [FromServices] ILogger<CommandController> logger)
    {
        var updateContext = updateContextCache.Retrieve();
        string commandText = updateContext.Update.Message!.Text!;

        if (commands.Switch(commandText) is Command command)
            await command.HandleAsync(updateContext, HttpContext.RequestAborted);
        else logger.LogTrace("{Command} command is unknown", commandText);
    }
}
