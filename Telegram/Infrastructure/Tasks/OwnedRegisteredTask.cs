using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure.Tasks;

internal record OwnedRegisteredTask(TypedRegisteredTask _, TelegramBot.User Owner)
{
}

static class OwnedRegisteredTaskExtensions
{
    internal static OwnedRegisteredTask OwnedBy(this TypedRegisteredTask registeredTask, TelegramBot.User user)
        => new(registeredTask, user);
}
