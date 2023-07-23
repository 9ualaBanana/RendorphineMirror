﻿using Microsoft.Extensions.Options;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Tasks;
using Telegram.Localization.Resources;
using Telegram.MPlus.Security;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

public class ImageProcessingCallbackQueryHandler
    : MediaProcessingCallbackQueryHandler<ImageProcessingCallbackQuery, ImageProcessingCallbackData>
{
    readonly TaskManager _taskManager;
    readonly Uri _hostUrl;

    public ImageProcessingCallbackQueryHandler(
        TaskManager taskManager,
        IOptions<TelegramBot.Options> botOptions,
        MediaFilesCache mediaFilesCache,
        LocalizedText.Media localizedMediaText,
        IHttpClientFactory httpClientFactory,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ImageProcessingCallbackQueryHandler> logger)
        : base(localizedMediaText, mediaFilesCache, httpClientFactory, serializer, bot, httpContextAccessor, logger)
    {
        _taskManager = taskManager;
        _hostUrl = botOptions.Value.Host;
    }

    protected override async Task HandleAsync(ImageProcessingCallbackQuery callbackQuery, MediaFilesCache.Entry cachedImage)
        => await (callbackQuery.Data switch
        {
            ImageProcessingCallbackData.UploadImage
                => UploadToMPlusAsync(cachedImage),
            ImageProcessingCallbackData.UpscaleImage | ImageProcessingCallbackData.UploadImage
                => UpscaleAndUploadToMPlusAsync(cachedImage),
            ImageProcessingCallbackData.VectorizeImage | ImageProcessingCallbackData.UploadImage
                => VectorizeAndUploadToMPlusAsync(cachedImage),
            _ => HandleUnknownCallbackData()
        });

    async Task UpscaleAndUploadToMPlusAsync(MediaFilesCache.Entry cachedImage)
    {
        var registeredTask = await _taskManager.TryRegisterAsync(
            new TaskCreationInfo(
                TaskAction.EsrganUpscale,
                new DownloadLinkTaskInputInfo(new Uri(_hostUrl, $"tasks/getinput/{cachedImage.Index}")),
                new MPlusTaskOutputInfo(cachedImage.Index.ToString(), "upscaled"),
                TaskObject.From(cachedImage.File)),
            new TelegramBot.User(ChatId, User),
            MPlusIdentity.SessionIdOf(User));

        if (registeredTask is not null)
            await Bot.SendMessageAsync_(ChatId, LocalizedMediaText.ResultPromise,
                new InlineKeyboardMarkup(DetailsButtonFor(registeredTask._))
                );
        else await Bot.SendMessageAsync_(ChatId, "Task couldn't be registered.");
    }

    async Task VectorizeAndUploadToMPlusAsync(MediaFilesCache.Entry cachedImage)
    {
        var registeredTask = await _taskManager.TryRegisterAsync(
            new TaskCreationInfo(
                TaskAction.VeeeVectorize,
                new DownloadLinkTaskInputInfo(new Uri(_hostUrl, $"tasks/getinput/{cachedImage.Index}")),
                new MPlusTaskOutputInfo(cachedImage.Index.ToString(), "vectorized"),
                new VeeeVectorizeInfo(new int[] { 8500 }),
                TaskObject.From(cachedImage.File)),
            new TelegramBot.User(ChatId, User),
            MPlusIdentity.SessionIdOf(User));

        if (registeredTask is not null)
            await Bot.SendMessageAsync_(ChatId, LocalizedMediaText.ResultPromise,
                new InlineKeyboardMarkup(DetailsButtonFor(registeredTask._))
                );
        else await Bot.SendMessageAsync_(ChatId, "Task couldn't be registered.");
    }

    InlineKeyboardButton DetailsButtonFor(ITypedRegisteredTask typedRegisteredTask)
        => InlineKeyboardButton.WithCallbackData("Details",
            Serializer.Serialize(new TaskCallbackQuery.Builder<TaskCallbackQuery>()
                .Data(TaskCallbackData.Details)
                .Arguments(typedRegisteredTask.Id, typedRegisteredTask.Action)
                .Build())
            );
}

public record ImageProcessingCallbackQuery : MediaProcessingCallbackQuery<ImageProcessingCallbackData>
{
}

[Flags]
public enum ImageProcessingCallbackData
{
    UploadImage = 1,
    UpscaleImage = 2,
    VectorizeImage = 4,
}
