using Telegram.Bot;

namespace Telegram.Infrastructure.Tasks;

internal record OwnedRegisteredTask(RegisteredTypedTask Task, TelegramBotUser Owner)
{
}

static class OwnedRegisteredTaskExtensions
{
    internal static OwnedRegisteredTask OwnedBy(this RegisteredTypedTask registeredTask, TelegramBotUser user)
        => new(registeredTask, user);
}
