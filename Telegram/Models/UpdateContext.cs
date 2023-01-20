using Telegram.Bot.Types;

namespace Telegram.Models;

// Authentication/Authorization info should be stored here as well and be populated by corresponding middleware.
// It likely should represent M+ user.
public class UpdateContext
{
    internal readonly Update Update;

    internal UpdateContext(Update update)
    {
        Update = update;
    }
}
