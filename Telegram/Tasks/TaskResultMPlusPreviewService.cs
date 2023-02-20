using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Models;
using Telegram.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Images.Models;

namespace Telegram.Tasks;

/// <summary>
/// <see cref="HttpClient"/> for sending task result previews from M+ to authenticated Telegram users.
/// </summary>
public class TaskResultMPlusPreviewService
{
    readonly MPlusService _mPlusService;
    readonly TelegramBot _bot;
    readonly RegisteredTasksCache _registeredTasksCache;
    readonly CachedFiles _cachedFiles;

    readonly ILogger _logger;

    public TaskResultMPlusPreviewService(
        MPlusService mPlusService,
        TelegramBot bot,
        RegisteredTasksCache registeredTasksCache,
        CachedFiles cachedFiles,
        ILogger<TaskResultMPlusPreviewService> logger)
    {
        _mPlusService = mPlusService;
        _bot = bot;
        _registeredTasksCache = registeredTasksCache;
        _cachedFiles = cachedFiles;
        _logger = logger;
    }

    internal async Task SendTaskResultPreviewsAsyncUsing(ITaskApi taskApi, string[] iids, string taskExecutor, CancellationToken cancellationToken)
    {
        try { await SendTaskResultPreviewsAsyncCoreUsing(taskApi, iids, taskExecutor, cancellationToken); }
        catch (Exception innerException)
        {
            var exception = new Exception($"Task result preview failed.", innerException);
            _logger.LogError(exception, message: default);
            throw exception;
        }
    }

    async Task SendTaskResultPreviewsAsyncCoreUsing(ITaskApi taskApi, string[] iids, string taskExecutor, CancellationToken cancellationToken)
    {
        if (_registeredTasksCache.Remove(taskApi.Id, out var authenticationToken))
        {
            var taskResultPreviews = new List<TaskResultPreviewFromMPlus>(iids.Length);
            foreach (var iid in iids)
                taskResultPreviews.Add(await RequestTaskResultPreviewAsyncUsing(taskApi, authenticationToken.MPlus.SessionId, iid, taskExecutor, cancellationToken));

            foreach (var taskResultPreview in taskResultPreviews)
                await SendTaskResultPreviewAsync(authenticationToken.ChatId, taskResultPreview);

            await Apis.DefaultWithSessionId(authenticationToken.MPlus.SessionId).ChangeStateAsync(taskApi, TaskState.Finished).ThrowIfError();
        }
        else throw new ArgumentException($"Task ID is unknown: {taskApi.Id}");
    }

    async Task<TaskResultPreviewFromMPlus> RequestTaskResultPreviewAsyncUsing(ITaskApi taskApi, string sessionId, string iid, string taskExecutor, CancellationToken cancellationToken)
    {
        var mPlusFileInfo = await _mPlusService.RequestFileInfoAsync(sessionId, iid, cancellationToken);
        var downloadLink = await _mPlusService.RequestFileDownloadLinkUsing(taskApi, sessionId, iid);
        return TaskResultPreviewFromMPlus.Create(mPlusFileInfo, taskExecutor, downloadLink);
    }

    async Task<Message> SendTaskResultPreviewAsync(ChatId chatId, TaskResultPreviewFromMPlus taskResultPreview)
    {
        int attemptsLeft = 3;
        Exception innerException = null!;
        while (attemptsLeft > 0)
        {
            try { return await SendTaskResultPreviewAsyncCore(chatId, taskResultPreview); }
            catch (Exception ex)
            {
                innerException = ex;
                attemptsLeft--;
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }

        var exception = new Exception($"IID {taskResultPreview.FileInfo.Iid}: Sending task result preview failed.", innerException);
        _logger.LogError(exception, message: default);
        throw exception;
    }

    async Task<Message> SendTaskResultPreviewAsyncCore(ChatId chatId, TaskResultPreviewFromMPlus taskResultPreview)
    {
        var cachedFileKey = _cachedFiles.Add(TelegramMediaFile.From(taskResultPreview.FileDownloadLink));
        string caption =
            $"{taskResultPreview.FileInfo.Title}\n\n" +
            $"*Task Executor* : `{taskResultPreview.TaskExecutor}`\n" +
            $"*Task ID* : `{taskResultPreview.TaskId}`\n" +
            $"*M+ IID* : `{taskResultPreview.FileInfo.Iid}`";
        var downloadButton = InlineKeyboardButton.WithUrl("Download", taskResultPreview.FileDownloadLink.ToString());
        var uploadToMPlusButton = InlineKeyboardButton.WithCallbackData("Upload to M+", ImageProcessingCallbackData.Serialize(ImageProcessingQueryFlags.UploadImage, cachedFileKey));
        var replyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[] { downloadButton, uploadToMPlusButton });

        return await (taskResultPreview switch
        {
            ImageTaskResultPreviewFromMPlus => _bot.SendImageAsync_(chatId, taskResultPreview, caption, replyMarkup),
            VideoTaskResultPreviewFromMPlus video => _bot.SendVideoAsync_(chatId, taskResultPreview,
                replyMarkup,
                null, video.Width, video.Height,
                taskResultPreview.FileInfo.MediumThumbnailUrl.ToString(),
                caption),
            _ => throw new NotImplementedException()
        });
    }
}
