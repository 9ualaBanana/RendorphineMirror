using Telegram.Bot;
using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure.Tasks;

internal record OwnedRegisteredTask(TypedRegisteredTask Task, TelegramBot.User Owner)
{
}

static class OwnedRegisteredTaskExtensions
{
    internal static OwnedRegisteredTask OwnedBy(this TypedRegisteredTask registeredTask, TelegramBot.User user)
        => new(registeredTask, user);
}
