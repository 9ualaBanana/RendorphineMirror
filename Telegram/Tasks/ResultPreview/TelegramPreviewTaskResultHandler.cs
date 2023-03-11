using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.CallbackQueries;
using Telegram.MediaFiles;
using Telegram.MPlus;
using File = System.IO.File;

namespace Telegram.Tasks.ResultPreview;

/// <summary>
/// <see cref="HttpClient"/> for sending task result previews from M+ to authenticated Telegram users.
/// </summary>
public class TelegramPreviewTaskResultHandler
{
    readonly MediaFilesCache _mediaFilesCache;
    readonly OwnedRegisteredTasksCache _ownedRegisteredTasksCache;
    readonly MPlusClient _mPlusClient;
    readonly TelegramBot _bot;

    readonly ILogger _logger;

    public TelegramPreviewTaskResultHandler(
        MediaFilesCache mediaFilesCache,
        OwnedRegisteredTasksCache ownedRegisteredTasksCache,
        MPlusClient mPlusClient,
        TelegramBot bot,
        ILogger<TelegramPreviewTaskResultHandler> logger)
    {
        _mediaFilesCache = mediaFilesCache;
        _ownedRegisteredTasksCache = ownedRegisteredTasksCache;
        _mPlusClient = mPlusClient;
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
                taskResultPreviews.Add(await RequestPreviewAsync(iid, taskOwner));

            foreach (var taskResultPreview in taskResultPreviews)
                await SendPreviewAsync(taskResultPreview, taskOwner.ChatId, cancellationToken);

            await Apis.Default.WithSessionId(MPlusIdentity.SessionIdOf(taskOwner.User))
                .ChangeStateAsync(executedTaskApi, TaskState.Finished).ThrowIfError();


            async Task<TaskResultPreviewFromMPlus> RequestPreviewAsync(string iid, TelegramBotUser taskOwner)
            {
                var mPlusMediaFile = new MPlusMediaFile(iid, MPlusIdentity.SessionIdOf(taskOwner.User));
                var mPlusFileInfo = await _mPlusClient.TaskManager.RequestFileInfoAsyncFor(mPlusMediaFile, cancellationToken);
                var downloadLink = await _mPlusClient.RequestFileDownloadLinkUsingFor(mPlusMediaFile, Extension.jpeg, executedTaskApi);
                return TaskResultPreviewFromMPlus.Create(mPlusFileInfo, executedTaskApi.Executor, downloadLink);
            }
        }
    }

    async Task<Message> SendPreviewAsync(TaskResultPreviewFromMPlus taskResultPreview, ChatId chatId, CancellationToken cancellationToken)
    {
        try { return await SendPreviewAsyncCore(); }
        catch (Exception ex)
        {
            var exception = new Exception($"IID {taskResultPreview.FileInfo.Iid}: Sending task result preview failed.", ex);
            _logger.LogError(exception, message: default);
            throw exception;
        }


        async Task<Message> SendPreviewAsyncCore()
        {
            var cachedTaskResult = await _mediaFilesCache.CacheAsync(MediaFile.From(taskResultPreview.FileDownloadLink), 1_800_000, cancellationToken);

            string caption =
                $"{taskResultPreview.FileInfo.Title}\n\n" +
                $"*Task Executor* : `{taskResultPreview.TaskExecutor}`\n" +
                $"*Task ID* : `{taskResultPreview.TaskId}`\n" +
                $"*M+ IID* : `{taskResultPreview.FileInfo.Iid}`";

            var downloadButton = InlineKeyboardButton.WithUrl("Download", taskResultPreview.FileDownloadLink.ToString());
            var replyMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[] { downloadButton });

            return await SendPreviewAsyncCore_();


            async Task<Message> SendPreviewAsyncCore_()
            {
                if (taskResultPreview is ImageTaskResultPreviewFromMPlus)
                {
                    var downscaledTaskResultPath = await Downscaled(cachedTaskResult);
                    try
                    {
                        using var downscaledTaskResult = File.OpenRead(downscaledTaskResultPath);
                        return await _bot.SendImageAsync_(chatId, downscaledTaskResult!, caption, replyMarkup, cancellationToken: cancellationToken);
                    }
                    finally { File.Delete(downscaledTaskResultPath); }
                }
                else if (taskResultPreview is VideoTaskResultPreviewFromMPlus video)
                    return await _bot.SendVideoAsync_(chatId, cachedTaskResult.File!,
                        replyMarkup,
                        null, video.Width, video.Height,
                        taskResultPreview.FileInfo.MediumThumbnailUrl.ToString(),
                        caption,
                        cancellationToken: cancellationToken);
                else throw new NotImplementedException();


                async Task<string> Downscaled(CachedMediaFile cachedImage)
                {
                    string downscaledImagePath = Path.ChangeExtension(Guid.NewGuid().ToString(), cachedImage.File.Extension.ToString());
                    using var cachedImageToDownscale = Image.Load(cachedImage.Path);
                    await cachedImageToDownscale
                        .Clone(x => x.Resize(cachedImageToDownscale.Width / 2, cachedImageToDownscale.Height / 2, KnownResamplers.Lanczos3))
                        .SaveAsync(downscaledImagePath, cancellationToken);
                    return downscaledImagePath;
                }
            }
        }
    }
}
