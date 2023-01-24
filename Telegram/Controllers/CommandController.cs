using Microsoft.AspNetCore.Mvc;
using Telegram.Models;

namespace Telegram.Controllers;

[ApiController]
[Route($"telegram/{PathFragment}")]
public class CommandController : UpdateControllerBase
{
    internal const string PathFragment = "command";

    public async Task Handle(
        [FromServices] IEnumerable<Command> commands,
        [FromServices] ILogger<CommandController> logger)
    {
        string commandText = UpdateContext.Update.Message!.Text!;

        if (commands.Switch(commandText) is Command command)
            await command.HandleAsync(UpdateContext, HttpContext.RequestAborted);
        else logger.LogTrace("{Command} command is unknown", commandText);
    }
}
