using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Tasks;
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
            var taskOwner = _ownedRegisteredTasksCache.Retrieve(TypedRegisteredTask.With(executedTaskApi.Id, executedTaskApi.Action)).Owner;
            var api = Apis.DefaultWithSessionId(MPlusIdentity.SessionIdOf(taskOwner.User));

            var taskResults = new List<TaskResultFromMPlus>(executedTaskApi.UploadedFiles.Count);
            foreach (var iid in executedTaskApi.UploadedFiles)
            {
                var fileAccessor = new MPlusFileAccessor(iid, MPlusIdentity.SessionIdOf(taskOwner.User));
                taskResults.Add(await _mPlusClient.RequestTaskResultAsyncUsing(executedTaskApi, fileAccessor, cancellationToken));
            }

            foreach (var taskResult in taskResults)
                await SendPreviewAsyncUsing(api, taskResult, taskOwner, cancellationToken);

            await api.ChangeStateAsync(executedTaskApi, TaskState.Finished).ThrowIfError();
        }
    }

    async Task<Message> SendPreviewAsyncUsing(Apis api, TaskResultFromMPlus taskResult, TelegramBotUser user, CancellationToken cancellationToken)
    {
        try { return await SendPreviewAsyncCore(); }
        catch (Exception ex)
        {
            var exception = new Exception($"IID {taskResult.FileInfo.Iid}: Sending task result preview failed.", ex);
            _logger.LogError(exception, message: default);
            throw exception;
        }


        async Task<Message> SendPreviewAsyncCore()
        {
            var cachedTaskResult = await _mediaFilesCache.CacheAsync(MediaFile.From(taskResult.FileDownloadLink), 1_800_000, cancellationToken);

            var caption = await BuildCaptionAsync();
            var replyMarkup = BuildReplyMarkup();

            return await SendPreviewAsyncCore_();


            async Task<string> BuildCaptionAsync()
            {
                var caption = new StringBuilder();

                var taskExecutionTime = await RequestTaskExecutionTime();

                if (!string.IsNullOrWhiteSpace(taskResult.FileInfo.Title))
                    caption.AppendLine(taskResult.FileInfo.Title);
                caption
                    .AppendLine($"{taskResult.Action} action has completed.")
                    .AppendLine($"*Duration*: `{taskExecutionTime}`")
                    .AppendLine($"*Size*: `{cachedTaskResult.Size / 1024 / 1024}` *MB*");

                if (MPlusIdentity.AccessLevelOf(user.User) is AccessLevel.Admin)
                    caption
                        .AppendLine($"*Task Executor* : `{taskResult.Executor}`")
                        .AppendLine($"*Task ID* : `{taskResult.Id}`")
                        .AppendLine($"*M+ IID* : `{taskResult.FileInfo.Iid}`");

                return caption.ToString();

                async Task<TimeSpan> RequestTaskExecutionTime()
                {
                    Exception exception = null!;
                    int attemptsLeft = 3;
                    while (attemptsLeft > 0)
                    {
                        try
                        {
                            var taskExecutionTime = (await api.GetTaskStateAsyncOrThrow(TaskApi.For(TypedRegisteredTask.With(taskResult.Id, taskResult.Action))).ThrowIfError()).Times.Total;
                            return taskExecutionTime!;
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                            attemptsLeft--;
                            Thread.Sleep(TimeSpan.FromSeconds(3));
                        }
                    }

                    throw exception;
                }
            }

            InlineKeyboardMarkup BuildReplyMarkup()
            {
                var downloadButton = InlineKeyboardButton.WithUrl("Download", taskResult.FileDownloadLink.ToString());
                return new InlineKeyboardMarkup(new InlineKeyboardButton[] { downloadButton });
            }

            async Task<Message> SendPreviewAsyncCore_()
            {
                if (taskResult is ImageTaskResultFromMPlus)
                {
                    var taskResultPreviewPath = await Downscaled(cachedTaskResult);
                    try
                    {
                        using var taskResultPreview = File.OpenRead(taskResultPreviewPath);
                        return await _bot.SendImageAsync_(user.ChatId, taskResultPreview!, caption, replyMarkup, cancellationToken: cancellationToken);
                    }
                    finally { File.Delete(taskResultPreviewPath); }
                }
                else if (taskResult is VideoTaskResultFromMPlus video)
                    return await _bot.SendVideoAsync_(user.ChatId, cachedTaskResult.File!,
                        replyMarkup,
                        null, video.Width, video.Height,
                        taskResult.FileInfo.MediumThumbnailUrl.ToString(),
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
