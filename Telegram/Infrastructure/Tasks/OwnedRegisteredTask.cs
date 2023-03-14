using System.Collections.Specialized;
using Telegram.Bot;

namespace Telegram.Infrastructure.Tasks;

internal record OwnedRegisteredTask(TypedRegisteredTask Task, TelegramBotUser Owner)
{
}

static class OwnedRegisteredTaskExtensions
{
    internal static OwnedRegisteredTask OwnedBy(this TypedRegisteredTask registeredTask, TelegramBotUser user)
        => new(registeredTask, user);
}
