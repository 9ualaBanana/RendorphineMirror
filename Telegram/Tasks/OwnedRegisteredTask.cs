namespace Telegram.Tasks;

internal record OwnedRegisteredTask(RegisteredTask Task, TelegramBotUser Owner)
{
}

static class OwnedRegisteredTaskExtensions
{
    internal static OwnedRegisteredTask OwnedBy(this RegisteredTask registeredTask, TelegramBotUser user)
        => new(registeredTask, user);
}
