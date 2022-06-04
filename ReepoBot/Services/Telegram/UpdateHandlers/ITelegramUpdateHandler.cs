using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.UpdateHandlers;

public interface ITelegramUpdateHandler : IEventHandler<Update>
{
}
