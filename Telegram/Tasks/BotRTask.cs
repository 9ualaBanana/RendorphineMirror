using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Localization.Resources;

namespace Telegram.Tasks;

public partial class BotRTask
{
    readonly LocalizedText.Media _localizedMediaText;
    readonly CallbackQuerySerializer _serializer;
    readonly TelegramBot _bot;
    readonly RTask _rTask;

    public BotRTask(LocalizedText.Media localizedMediaText, CallbackQuerySerializer serializer, TelegramBot bot, RTask rTask)
    {
        _localizedMediaText = localizedMediaText;
        _serializer = serializer;
        _bot = bot;
        _rTask = rTask;
    }

    internal async Task<OwnedRegisteredTask?> TryRegisterAsync(TaskCreationInfo taskInfo, TelegramBot.User taskOwner)
    {
        if (await _rTask.TryRegisterAsync(taskInfo, taskOwner) is OwnedRegisteredTask ownedRegisteredTask)
        {
            await _bot.SendMessageAsync_(taskOwner.ChatId, _localizedMediaText.ResultPromise, DetailsButton());
            return ownedRegisteredTask;
        }
        else
        {
            await _bot.SendMessageAsync_(taskOwner.ChatId, $"Task couldn't be registered: no more free {taskInfo.Action} actions left.");
            return null;
        }


        InlineKeyboardMarkup DetailsButton()
            => new(InlineKeyboardButton.WithCallbackData("Details",
                _serializer.Serialize(new TaskCallbackQuery.Builder<TaskCallbackQuery>()
                    .Data(TaskCallbackData.Details)
                    .Arguments(ownedRegisteredTask.Id, ownedRegisteredTask.Action)
                    .Build())
                )
            );
    }
}
