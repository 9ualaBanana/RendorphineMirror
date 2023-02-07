using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Models;
using Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates.Tasks;

public class TaskResultPreviewService
{
	readonly MPlusService _mPlusService;
	readonly RegisteredTasksCache _registeredTasksCache;
	readonly TelegramBot _bot;

	readonly ILogger _logger;

	public TaskResultPreviewService(MPlusService mPlusService, RegisteredTasksCache registeredTasksCache, TelegramBot bot, ILogger<TaskResultPreviewService> logger)
	{
		_mPlusService = mPlusService;
		_registeredTasksCache = registeredTasksCache;
		_bot = bot;
		_logger = logger;
	}

	internal async Task SendTaskResultPreviewsAsyncUsing(ITaskApi taskApi, string[] iids, string taskExecutor, CancellationToken cancellationToken)
	{
        if (_registeredTasksCache.Remove(taskApi.Id, out var authenticationToken))
        {
			var taskResultPreviews = new List<TaskResultPreview>(iids.Length);
            foreach (var iid in iids)
                taskResultPreviews.Add(await RequestTaskResultPreviewAsyncUsing(taskApi, authenticationToken.MPlus.SessionId, iid, taskExecutor, cancellationToken));

			foreach (var taskResultPreview in taskResultPreviews)
				await _bot.SendTaskResultPreviewAsync(authenticationToken.ChatId, taskResultPreview);

            await Apis.Default.WithSessionId(authenticationToken.MPlus.SessionId).ChangeStateAsync(taskApi, TaskState.Finished).ThrowIfError();
        }
    }

	async Task<TaskResultPreview> RequestTaskResultPreviewAsyncUsing(ITaskApi taskApi, string sessionId, string iid, string taskExecutor, CancellationToken cancellationToken)
	{
		var mPlusFileInfo = await _mPlusService.RequestFileInfoAsync(sessionId, iid, cancellationToken);
		var downloadLink = await _mPlusService.RequestFileDownloadLinkUsing(taskApi, sessionId, iid);
		return TaskResultPreview.Create(mPlusFileInfo, taskExecutor, downloadLink);
	}
}

static class TaskResultPreviewServiceExtensions
{
    internal static async Task<Message> SendTaskResultPreviewAsync(this TelegramBot bot, ChatId chatId, TaskResultPreview taskResultPreview)
	{
        string caption =
			$"{taskResultPreview.FileInfo.Title}\n\n" +
			$"*Task Executor* : `{taskResultPreview.TaskExecutor}`\n" +
			$"*Task ID* : `{taskResultPreview.TaskId}`\n" +
			$"*M+ IID* : `{taskResultPreview.FileInfo.Iid}`";
        var downloadButton = new InlineKeyboardMarkup( InlineKeyboardButton.WithUrl("Download", taskResultPreview.FileDownloadLink.ToString()) );

		return await (taskResultPreview switch
		{
			ImageTaskResultPreview => bot.SendImageAsync_(chatId, taskResultPreview, caption, downloadButton),
			VideoTaskResultPreview video => bot.SendVideoAsync_(chatId, taskResultPreview,
				downloadButton,
				null, video.Width, video.Height,
				taskResultPreview.FileInfo.BigThumbnailUrl.ToString(),
				caption),
			_ => throw new NotImplementedException()
		});
    }
}
