using Telegram.Infrastructure.Bot;

namespace Telegram.Tasks;

internal record OwnedRegisteredTask : TypedRegisteredTask
{
    internal TelegramBot.User Owner { get; }

    internal OwnedRegisteredTask(TypedRegisteredTask original, TelegramBot.User owner)
        : base(original)
    {
        Owner = owner;
    }
}

static class OwnedRegisteredTaskExtensions
{
    internal static OwnedRegisteredTask OwnedBy(this TypedRegisteredTask registeredTask, TelegramBot.User user)
        => new(registeredTask, user);
}
