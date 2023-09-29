using Microsoft.Extensions.Caching.Memory;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Localization.Resources;
using Telegram.Tasks;

namespace Telegram.Localization;

public abstract partial class Notifications
{
    public class RTask : Notifications
    {
        readonly CachedMessages _sentTaskDetailsMessages;
        readonly LocalizedText.Media _localizedMediaText;

        public RTask(CachedMessages sentTaskDetailsMessages, LocalizedText.Media localizedMediaText, TelegramBot bot, CallbackQuerySerializer serializer)
            : base(bot, serializer)
        {
            _sentTaskDetailsMessages = sentTaskDetailsMessages;
            _localizedMediaText = localizedMediaText;
        }

        internal async Task<Message> SendResultPromiseAsyncFor(OwnedRegisteredTask ownedRegisteredTask, CancellationToken cancellationToken)
        {
            return await Bot.SendMessageAsync_(ownedRegisteredTask.Owner.ChatId, _localizedMediaText.ResultPromise, DetailsButton(), cancellationToken: cancellationToken);


            InlineKeyboardMarkup DetailsButton()
                => new(InlineKeyboardButton.WithCallbackData("Details",
                    Serializer.Serialize(new RTaskCallbackQuery.Builder<RTaskCallbackQuery>()
                    .Data(RTaskCallbackData.Details)
                    .Arguments(ownedRegisteredTask.Id, ownedRegisteredTask.Action)
                    .Build())
                ));
        }

        internal async Task<Message> SendRegistrationFailedAsyncFor(ChatId chatId, TaskCreationInfo rTaskInfo, CancellationToken cancellationToken)
            => await Bot.SendMessageAsyncCore(chatId, $"Task couldn't be registered: no more free {rTaskInfo.Action} actions left.", cancellationToken: cancellationToken);

        internal async Task<Message> SendDetailsAsync(ChatId chatId, string sessionId, RTaskCallbackQuery callbackQuery, Message callbackQuerySource, CancellationToken cancellationToken)
        {
            var details = await Details();

            if (_sentTaskDetailsMessages.ProducedBy(callbackQuerySource) is Message sentTaskDetailsMessage)
                return await Bot.EditMessageAsync_(chatId, sentTaskDetailsMessage.MessageId, details, cancellationToken: cancellationToken);
            else
            {
                sentTaskDetailsMessage = await Bot.SendMessageAsync_(chatId, details, cancellationToken: cancellationToken);
                _sentTaskDetailsMessages.Add(callbackQuerySource, sentTaskDetailsMessage);
                return sentTaskDetailsMessage;
            }


            async Task<string> Details()
            {
                var taskState = await TaskApi.For(RegisteredTask.With(callbackQuery.TaskId)).With(sessionId).GetStateAsync();
                var details = new StringBuilder()
                    .AppendLine($"*Action* : `{callbackQuery.Action}`")
                    .AppendLine($"*Task ID* : `{callbackQuery.TaskId}`")
                    .AppendLine($"*State* : `{taskState.State}`");
                if (taskState.Times.Exist)
                {
                    details.AppendLine($"*Duration* : `{taskState.Times.Total}`");
                    details.AppendLine($"*Server* : `{taskState.Server}`");
                }
                return details.ToString();
            }
        }

        internal async Task<Message> SendDetailsUnavailableAsync(ChatId chatId, CancellationToken cancellationToken)
            => await Bot.SendMessageAsync_(chatId, "Task details are currently unavailable.", cancellationToken: cancellationToken);


        // Consider moving to base class.
        public class CachedMessages
        {
            readonly IMemoryCache _cache;

            public CachedMessages(IMemoryCache cache)
            {
                _cache = cache;
            }

            internal void Add(Message callbackQuerySource, Message details)
                => _cache.CreateEntry(UniqueMessage.From(callbackQuerySource))
                .SetValue(details)
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(60))
                .Dispose();

            internal Message? ProducedBy(Message callbackQuerySource)
                => _cache.TryGetValue(UniqueMessage.From(callbackQuerySource), out Message? details) ?
                details : null;
        }
    }
}
