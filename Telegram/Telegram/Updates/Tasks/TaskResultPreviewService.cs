using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

namespace Telegram.Telegram.Updates.Tasks;

public class TaskResultPreviewService
{
	readonly HttpClient _httpClient;

	readonly ILogger _logger;

	public TaskResultPreviewService(HttpClient httpClient, ILogger<TaskResultPreviewService> logger)
	{
		_httpClient = httpClient;
		_logger = logger;
	}

	internal async Task<TaskResultPreview> RequestTaskResultPreviewAsyncUsing(ITaskApi taskApi, string sessionId, string iid, string taskExecutor, CancellationToken cancellationToken)
	{
		var mPlusFileInfo = await RequestMPlusFileInfoAsync(sessionId, iid, cancellationToken);
		var downloadLink = await RequestDownloadLinkUsing(taskApi, sessionId, iid);
		return TaskResultPreview.Create(mPlusFileInfo, taskExecutor, downloadLink);
	}

	async Task<MPlusFileInfo> RequestMPlusFileInfoAsync(string sessionId, string iid, CancellationToken cancellationToken)
	{
		var endpoint = new Uri(new Uri(Api.TaskManagerEndpoint), "getmympitem").ToString();
		var requestUrl = QueryHelpers.AddQueryString(endpoint, new Dictionary<string, string?> { { "sessionId", sessionId }, { "iid", iid } });

		return await RequestMPlusFileInfoAsyncCore(cancellationToken);


		async Task<MPlusFileInfo> RequestMPlusFileInfoAsyncCore(CancellationToken cancellationToken)
		{
			int attemptsLeft = 3;
            JToken mPlusFileInfo;
            while (attemptsLeft > 0)
			{
                mPlusFileInfo = (await (await _httpClient.GetAsync(requestUrl, cancellationToken)).GetJsonIfSuccessfulAsync())["item"]!;
				if ((string)mPlusFileInfo["state"]! == "received")
					return MPlusFileInfo.From(mPlusFileInfo);
				else Thread.Sleep(TimeSpan.FromSeconds(3));
            }

			var exception = new Exception("Couldn't request {FileInfo} for the file with IID {Iid}.");
			_logger.LogError(exception, message: default);
			throw exception;
        }
    }

	async Task<Uri> RequestDownloadLinkUsing(ITaskApi taskApi, string sessionId, string iid)
		=> new Uri((await taskApi.GetMPlusItemDownloadLinkAsync(iid, sessionId)).ThrowIfError());
}

static class TaskResultPreviewServiceExtensions
{
	internal static async Task SendTaskResultPreviewAsync(this TelegramBot bot, ChatId chatId, TaskResultPreview taskResultPreview)
	{
		var inputOnlineFile = new InputOnlineFile(taskResultPreview.FileDownloadLink);
        string caption = $"{taskResultPreview.FileInfo.Title}\\n\\n*Task Executor* : `{taskResultPreview.TaskExecutor}`\\n*Task ID* : `{taskResultPreview.TaskId}`\\n*M+ IID* : `{taskResultPreview.FileInfo.Iid}`";
        var downloadButton = new InlineKeyboardMarkup( InlineKeyboardButton.WithUrl("Download", taskResultPreview.FileDownloadLink.ToString()) );

		await (taskResultPreview switch
		{
			ImageTaskResultPreview => bot.SendImageAsync_(chatId, inputOnlineFile, caption, downloadButton),
			VideoTaskResultPreview video => bot.SendVideoAsync_(chatId, inputOnlineFile, width: video.Width, height: video.Height, thumb: taskResultPreview.FileInfo.BigThumbnailUrl.ToString(), caption: caption, replyMarkup: downloadButton),
			_ => throw new NotImplementedException()
		});
    }
}
