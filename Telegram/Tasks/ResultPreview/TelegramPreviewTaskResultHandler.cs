using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Media;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Tasks;
using Telegram.Models;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Files;
using Telegram.MPlus.Security;
using File = System.IO.File;

namespace Telegram.Tasks.ResultPreview;

/// <summary>
/// <see cref="HttpClient"/> for sending task result previews from M+ to authenticated Telegram users.
/// </summary>
public class TelegramPreviewTaskResultHandler
{
    readonly MediaFilesCache _mediaFilesCache;
    readonly MediaFile.Factory _mediaFileFactory;
    readonly OwnedRegisteredTasksCache _ownedRegisteredTasksCache;
    readonly MPlusClient _mPlusClient;
    readonly TelegramBot _bot;

    readonly ILogger _logger;

    public TelegramPreviewTaskResultHandler(
        MediaFilesCache mediaFilesCache,
        MediaFile.Factory mediaFileFactory,
        OwnedRegisteredTasksCache ownedRegisteredTasksCache,
        MPlusClient mPlusClient,
        TelegramBot bot,
        ILogger<TelegramPreviewTaskResultHandler> logger)
    {
        _mediaFilesCache = mediaFilesCache;
        _mediaFileFactory = mediaFileFactory;
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
            var exception = new Exception(LogId.Formatted("Task result preview failed.", executedTaskApi.Id, "TaskID"), ex);
            _logger.LogError(exception, message: default);
            throw exception;
        }


        async Task SendPreviewsAsyncCore()
        {
            var taskOwner = _ownedRegisteredTasksCache.Retrieve(executedTaskApi as ExecutedTask).Owner;
            var api = Apis.DefaultWithSessionId(MPlusIdentity.SessionIdOf(taskOwner));

            foreach (var taskResult in await RequestTaskResultsAsyncFor(executedTaskApi.UploadedFiles).ToArrayAsync(cancellationToken))
                await SendPreviewAsyncUsing(api, taskResult, taskOwner, cancellationToken);

            await api.ChangeStateAsync(executedTaskApi, TaskState.Finished).ThrowIfError();


            async IAsyncEnumerable<TaskResultFromMPlus> RequestTaskResultsAsyncFor(IEnumerable<string> uploadedFiles)
            {
                foreach (var iid in uploadedFiles)
                {
                    var fileAccessor = new MPlusFileAccessor(iid, MPlusIdentity.SessionIdOf(taskOwner));
                    yield return await _mPlusClient.RequestTaskResultAsyncUsing(api, executedTaskApi, fileAccessor, cancellationToken);
                };
            }
        }
    }

    async Task<Message> SendPreviewAsyncUsing(Apis api, TaskResultFromMPlus taskResult, TelegramBot.User user, CancellationToken cancellationToken)
    {
        try { return await SendPreviewAsync(); }
        catch (Exception ex)
        {
            var errorMessage = "Task result preview could not be sent.";
            var exception = new Exception(LogId.Formatted(errorMessage, taskResult.FileInfo.Iid, "IID"), ex);
            _logger.LogError(exception, message: default);
            throw exception;
        }


        async Task<Message> SendPreviewAsync()
        {
            var cachedTaskResult = await _mediaFilesCache.AddAsync(
                await _mediaFileFactory.CreateAsyncFrom(taskResult.FileDownloadLink, cancellationToken), TimeSpan.FromMinutes(30), cancellationToken);
            var cachedTaskPreview = taskResult.Action is not TaskAction.VeeeVectorize ?
                cachedTaskResult : await _mediaFilesCache.AddAsync(
                    await _mediaFileFactory.CreateAsyncFrom(taskResult.PreviewDownloadLink, cancellationToken), cancellationToken);

            var caption = await BuildCaptionAsync();
            var replyMarkup = BuildReplyMarkupAsync();

            return await SendPreviewAsyncCore();


            async Task<string> BuildCaptionAsync()
            {
                var caption = new StringBuilder();

                var taskExecutionTime = await RequestTaskExecutionTime();

                caption
                    .AppendLine($"*{taskResult.Action}* action has completed.")
                    .AppendLine();
                if (!string.IsNullOrWhiteSpace(taskResult.FileInfo.Title))
                    caption.AppendLine($"*Title* : {taskResult.FileInfo.Title}");
                caption
                    .AppendLine($"*Size*: `{cachedTaskResult.File.Length / 1024 / 1024}` *MB*")
                    .AppendLine($"*Execution Time*: `{taskExecutionTime}`");

                if (MPlusIdentity.AccessLevelOf(user) is AccessLevel.Admin)
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

            InlineKeyboardMarkup BuildReplyMarkupAsync()
            {
                var downloadButton = InlineKeyboardButton.WithUrl("Download", taskResult.FileDownloadLink.ToString());
                return new InlineKeyboardMarkup(new InlineKeyboardButton[] { downloadButton });
            }

            async Task<Message> SendPreviewAsyncCore()
            {
                if (taskResult is ImageTaskResultFromMPlus)
                {
                    var taskResultPreviewPath = taskResult.Action is not TaskAction.VeeeVectorize ?
                        await PathToDownscaled(cachedTaskPreview.File) : cachedTaskPreview.File.FullName;
                    try
                    {
                        using var taskResultPreview = File.OpenRead(taskResultPreviewPath);
                        return await _bot.SendImageAsync_(user.ChatId, taskResultPreview!, caption, replyMarkup, cancellationToken: cancellationToken);
                    }
                    finally { File.Delete(taskResultPreviewPath); }
                }
                else if (taskResult is VideoTaskResultFromMPlus video)
                    return await _bot.SendVideoAsync_(user.ChatId, cachedTaskResult.File.OpenRead()!,
                        replyMarkup,
                        null, video.Width, video.Height,
                        taskResult.FileInfo.MediumThumbnailUrl.ToString(),
                        caption,
                        cancellationToken: cancellationToken);
                else throw new NotImplementedException();


                async Task<string> PathToDownscaled(FileInfo cachedImage)
                {
                    string downscaledImagePath = Path.ChangeExtension(
                        $"{Path.GetFileNameWithoutExtension(cachedImage.FullName)}_downscaled", cachedImage.Extension.ToString()
                        );
                    using var cachedImageToDownscale = Image.Load(cachedImage.FullName);
                    await cachedImageToDownscale
                        .Clone(image => image.Resize(cachedImageToDownscale.Width / 2, cachedImageToDownscale.Height / 2, KnownResamplers.Lanczos3))
                        .SaveAsync(downscaledImagePath, cancellationToken);
                    return downscaledImagePath;
                }
            }
        }
    }
}
