using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.CallbackQueries;
using Telegram.MediaFiles;
using Telegram.MediaFiles.Images;
using Telegram.MPlus;

namespace Telegram.Tasks.ResultPreview;

/// <summary>
/// <see cref="HttpClient"/> for sending task result previews from M+ to authenticated Telegram users.
/// </summary>
public class TelegramPreviewTaskResultHandler
{
    readonly MediaFilesCache _mediaFilesCache;
    readonly OwnedRegisteredTasksCache _ownedRegisteredTasksCache;
    readonly MPlusClient _mPlusClient;
    readonly CallbackQuerySerializer _callbackQuerySerializer;
    readonly TelegramBot _bot;

    readonly ILogger _logger;

    public TelegramPreviewTaskResultHandler(
        MediaFilesCache mediaFilesCache,
        OwnedRegisteredTasksCache ownedRegisteredTasksCache,
        MPlusClient mPlusClient,
        CallbackQuerySerializer callbackQuerySerializer,
        TelegramBot bot,
        ILogger<TelegramPreviewTaskResultHandler> logger)
    {
        _mediaFilesCache = mediaFilesCache;
        _ownedRegisteredTasksCache = ownedRegisteredTasksCache;
        _mPlusClient = mPlusClient;
        _callbackQuerySerializer = callbackQuerySerializer;
        _bot = bot;
        _logger = logger;
    }

    internal async Task SendPreviewAsyncUsing(ExecutedTaskApi executedTaskApi, CancellationToken cancellationToken)
    {
        try { await SendPreviewsAsyncCore(); }
        catch (Exception ex)
        {
            var exception = new Exception($"Task result preview failed.", ex);
            _logger.LogError(exception, message: default);
            throw exception;
        }


        async Task SendPreviewsAsyncCore()
        {
            var taskOwner = _ownedRegisteredTasksCache.Retrieve(RegisteredTask.With(executedTaskApi.Id)).Owner;
            var taskResultPreviews = new List<TaskResultPreviewFromMPlus>(executedTaskApi.UploadedFiles.Count);
            foreach (var iid in executedTaskApi.UploadedFiles)
                taskResultPreviews.Add(await RequestPreviewAsyncUsing(executedTaskApi, iid, taskOwner, cancellationToken));

            foreach (var taskResultPreview in taskResultPreviews)
                await SendPreviewAsync(taskResultPreview, taskOwner.ChatId, cancellationToken);

            await Apis.Default.WithSessionId(MPlusIdentity.SessionIdOf(taskOwner.User))
                .ChangeStateAsync(executedTaskApi, TaskState.Finished).ThrowIfError();
        }
    }

    async Task<TaskResultPreviewFromMPlus> RequestPreviewAsyncUsing(ExecutedTaskApi executedTaskApi, string iid, TelegramBotUser taskOwner, CancellationToken cancellationToken)
    {
        var mPlusMediaFile = new MPlusMediaFile(iid, MPlusIdentity.SessionIdOf(taskOwner.User));
        var mPlusFileInfo = await _mPlusClient.TaskManager.RequestFileInfoAsyncFor(mPlusMediaFile, cancellationToken);
        var downloadLink = await _mPlusClient.RequestFileDownloadLinkUsingFor(mPlusMediaFile, Extension.jpeg, executedTaskApi);
        return TaskResultPreviewFromMPlus.Create(mPlusFileInfo, executedTaskApi.Executor, downloadLink);
    }
    //
    async Task<Message> SendPreviewAsync(TaskResultPreviewFromMPlus taskResultPreview, ChatId chatId, CancellationToken cancellationToken)
    {
        try { return await SendPreviewAsyncCore(taskResultPreview, chatId, cancellationToken); }
        catch (Exception ex)
        {
            var exception = new Exception($"IID {taskResultPreview.FileInfo.Iid}: Sending task result preview failed.", ex);
            _logger.LogError(exception, message: default);
            throw exception;
        }
    }

    async Task<Message> SendPreviewAsyncCore(TaskResultPreviewFromMPlus taskResultPreview, ChatId chatId, CancellationToken cancellationToken)
    {
        var cachedTaskResult = await _mediaFilesCache.AddAsync(MediaFile.From(taskResultPreview.FileDownloadLink), cancellationToken);

        string caption =
            $"{taskResultPreview.FileInfo.Title}\n\n" +
            $"*Task Executor* : `{taskResultPreview.TaskExecutor}`\n" +
            $"*Task ID* : `{taskResultPreview.TaskId}`\n" +
            $"*M+ IID* : `{taskResultPreview.FileInfo.Iid}`";

        var downloadButton = InlineKeyboardButton.WithUrl("Download", taskResultPreview.FileDownloadLink.ToString());
        var uploadToMPlusButton = InlineKeyboardButton.WithCallbackData("Upload to M+",
            _callbackQuerySerializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
            .Data(ImageProcessingCallbackData.UploadImage)
            .Arguments(cachedTaskResult.Index)
            .Build()));
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
