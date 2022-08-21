using Common;
using ReepoBot.Services.Telegram.Authentication;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates.Images;

public class TaskCallbackQueryHandler
{
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly TelegramChatIdAuthentication _authentication;

    public TaskCallbackQueryHandler(
        ILogger<TaskCallbackQueryHandler> logger,
        TelegramBot bot,
        TelegramChatIdAuthentication authentication)
    {
        _logger = logger;
        _bot = bot;
        _authentication = authentication;
    }

    public async Task HandleAsync(Update update)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        var authenticationToken = _authentication.GetTokenFor(chatId);
        if (authenticationToken is null) return;

        var taskCallbackData = new TaskCallbackData(update.CallbackQuery.Data!);

        if (taskCallbackData.Value.HasFlag(TaskQueryFlags.Details))
        {
            var taskState = await Apis.GetTaskStateAsync(taskCallbackData.TaskId, authenticationToken.SessionId);
            if (taskState)
            {
                await _bot.TrySendMessageAsync(chatId, $"TaskID: *{taskCallbackData.TaskId}*\nState: *{taskState.Result.State}*\nProgress: *{taskState.Result.Progress}*\nServer: *{taskState.Result.Server}*");
            }
            else
            {
                await _bot.TrySendMessageAsync(chatId, "Couldn't get task progress.");
            }
        }
    }
}
