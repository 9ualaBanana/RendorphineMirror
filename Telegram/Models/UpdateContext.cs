using System.Security.Claims;
using Telegram.Bot.Types;

namespace Telegram.Models;

/// <summary>
/// Stores all <see cref="Bot.Types.Update"/> related data populated by <see cref="Bot.Types.Update"/>-routing middleware.
/// </summary>
/// <remarks>
/// Instances of <see cref="UpdateContext"/> are constructed by <see cref="UpdateContextConstructorMiddleware"/>
/// and used by the rest of <see cref="Bot.Types.Update"/>-routing middleware to make routing decisions against it.
/// </remarks>
public class UpdateContext
{
    internal readonly Update Update;
    internal ClaimsPrincipal User { get; set; }

    internal UpdateContext(Update update)
    {
        Update = update;
        User = new ClaimsPrincipal();
    }
}
