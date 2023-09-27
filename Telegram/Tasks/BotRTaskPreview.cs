using NodeCommon.ApiModel;
using Polly;
using System.Text;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Tasks;
using Telegram.Models;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;
using Telegram.Tasks.ResultPreview;
using File = System.IO.File;

namespace Telegram.Tasks;

public class BotRTaskPreview
{
    readonly MediaFilesCache _mediaFilesCache;
    readonly MediaFile.Factory _mediaFileFactory;
    readonly OwnedRegisteredTasksCache _ownedRegisteredTasksCache;
    readonly MPlusTaskManagerClient _taskManager;
    readonly TelegramBot _bot;

    readonly ILogger _logger;

    public BotRTaskPreview(
        MediaFilesCache mediaFilesCache,
        MediaFile.Factory mediaFileFactory,
        OwnedRegisteredTasksCache ownedRegisteredTasksCache,
        MPlusTaskManagerClient taskManager,
        TelegramBot bot,
        ILogger<BotRTaskPreview> logger)
    {
        _mediaFilesCache = mediaFilesCache;
        _mediaFileFactory = mediaFileFactory;
        _ownedRegisteredTasksCache = ownedRegisteredTasksCache;
        _taskManager = taskManager;
        _bot = bot;
        _logger = logger;
    }

    internal async Task SendAsyncUsing(ExecutedRTaskApi executedRTask, CancellationToken cancellationToken)
    {
        try { await SendAsyncCore(); }
        catch (Exception ex)
        {
            var exception = new Exception(LogId.Formatted("Task result preview failed.", executedRTask.Id, "TaskID"), ex);
            _logger.LogError(exception, message: default);
            throw exception;
        }


        async Task SendAsyncCore()
        {
            var rTaskOwner = _ownedRegisteredTasksCache.Retrieve(executedRTask as ExecutedRTask).Owner;
            var userExecutedRTask = executedRTask.With(MPlusIdentity.SessionIdOf(rTaskOwner));
            await foreach (var rTaskResult in _taskManager.ObtainResultsAsyncOf(userExecutedRTask, cancellationToken))
                await SendAsync(rTaskResult);
            await userExecutedRTask.ChangeStateAsyncTo(TaskState.Finished);


            async Task<Message> SendAsync(RTaskResult.MPlus rTaskResult)
            {
                try { return await SendPreviewAsync(); }
                catch (Exception ex)
                {
                    var errorMessage = "Task result preview could not be sent.";
                    var exception = new Exception(LogId.Formatted(errorMessage, rTaskResult.FileInfo.Iid, "IID"), ex);
                    _logger.LogError(exception, message: default);
                    throw exception;
                }


                async Task<Message> SendPreviewAsync()
                {
                    var cachedTaskResult = await _mediaFilesCache.AddAsync(
                        await _mediaFileFactory.CreateAsyncFrom(rTaskResult.FileDownloadLink, cancellationToken),
                        TimeSpan.FromMinutes(30), cancellationToken);

                    var cachedTaskPreview = rTaskResult.Action is not TaskAction.VeeeVectorize ? cachedTaskResult :
                        await _mediaFilesCache.AddAsync(
                            await _mediaFileFactory.CreateAsyncFrom(rTaskResult.PreviewDownloadLink, cancellationToken),
                            cancellationToken);

                    var caption = await BuildCaptionAsync();
                    var replyMarkup = ReplyMarkup();

                    return await SendPreviewAsyncCore();


                    async Task<string> BuildCaptionAsync()
                    {
                        var caption = new StringBuilder();

                        caption
                            .AppendLine($"*{rTaskResult.Action}* action has completed.")
                            .AppendLine();
                        if (!string.IsNullOrWhiteSpace(rTaskResult.FileInfo.Title))
                            caption.AppendLine($"*Title* : {rTaskResult.FileInfo.Title}");
                        caption
                            .AppendLine($"*Size*: `{cachedTaskResult.File.Length / 1024 / 1024}` *MB*")
                            .AppendLine($"*Execution Time*: `{await RequestTaskExecutionTimeAsync()}`");

                        if (MPlusIdentity.AccessLevelOf(rTaskOwner) is AccessLevel.Admin)
                            caption
                                .AppendLine($"*Task Executor* : `{rTaskResult.Executor}`")
                                .AppendLine($"*Task ID* : `{rTaskResult.Id}`")
                                .AppendLine($"*M+ IID* : `{rTaskResult.FileInfo.Iid}`");

                        return caption.ToString();


                        async Task<TimeSpan> RequestTaskExecutionTimeAsync()
                            => (await Policy<ServerTaskState>.Handle<Exception>()
                                .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(1.5 * attempt))
                                .ExecuteAsync(() => userExecutedRTask.GetStateAsync().AsTask()))
                                .Times.Total;
                    }

                    InlineKeyboardMarkup ReplyMarkup()
                    {
                        var downloadButton = InlineKeyboardButton.WithUrl("Download", rTaskResult.FileDownloadLink.ToString());
                        return new InlineKeyboardMarkup(new InlineKeyboardButton[] { downloadButton });
                    }

                    async Task<Message> SendPreviewAsyncCore()
                    {
                        if (rTaskResult is RTaskResult.MPlus.Image)
                        {
                            var taskResultPreviewPath = rTaskResult.Action is not TaskAction.VeeeVectorize ?
                                await PathToDownscaled(cachedTaskPreview.File) : cachedTaskPreview.File.FullName;
                            try
                            {
                                using var taskResultPreview = File.OpenRead(taskResultPreviewPath);
                                return await _bot.SendImageAsync_(rTaskOwner.ChatId, taskResultPreview!, caption, replyMarkup, cancellationToken: cancellationToken);
                            }
                            finally { File.Delete(taskResultPreviewPath); }
                        }
                        else if (rTaskResult is RTaskResult.MPlus.Video video)
                            return await _bot.SendVideoAsync_(rTaskOwner.ChatId, cachedTaskResult.File.OpenRead()!,
                                replyMarkup,
                                null, video.Width, video.Height,
                                rTaskResult.FileInfo.MediumThumbnailUrl.ToString(),
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
    }
}
