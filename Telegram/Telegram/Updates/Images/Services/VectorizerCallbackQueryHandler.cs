﻿using Common.Plugins;
using Common.Tasks.Info;
using Common.Tasks.Model;
using Common.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Services.Telegram.FileRegistry;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Images.Models;
using Telegram.Telegram.Updates.Tasks.Models;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates.Images.Services;

public class VectorizerCallbackQueryHandler : MediaFileProcessingCallbackQueryHandler
{
    readonly TaskRegistry _taskRegistry;
    readonly string _hostUrl;

    public VectorizerCallbackQueryHandler(
        ILogger<VectorizerCallbackQueryHandler> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        TaskRegistry taskRegistry,
        TelegramFileRegistry fileRegistry,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
        : base(logger, bot, authenticator, fileRegistry, httpClientFactory)
    {
        _taskRegistry = taskRegistry;
        _hostUrl = configuration["Host"];
    }

    protected override async Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken) =>
        await HandleAsync(update, authenticationToken, new VectorizerCallbackData(CallbackDataFrom(update)));

    protected override async Task Process<T>(
        Update update,
        ChatAuthenticationToken authenticationToken,
        MediaFileProcessingCallbackData<T> mediaFileProcessingCallbackData,
        string mediaFilePath)
    {
        if (mediaFileProcessingCallbackData.Value.HasFlag(VectorizerQueryFlags.Vectorize))
            await VectorizeAndUploadToMPlusAsync(ChatIdFrom(update), (mediaFileProcessingCallbackData as VectorizerCallbackData)!, authenticationToken);
    }

    async Task VectorizeAndUploadToMPlusAsync(ChatId chatId, VectorizerCallbackData vectorizerCallbackData, ChatAuthenticationToken authenticationToken)
    {
        var taskId = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                PluginType.VeeeVectorizer,
                "VeeeVectorize",
                default,
                new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{vectorizerCallbackData.FileRegistryKey}"),
                new MPlusTaskOutputInfo($"{vectorizerCallbackData.FileRegistryKey}", "vectorized"),
                JObject.FromObject(new VeeeVectorizeInfo() { Lods = new int[] { vectorizerCallbackData.Polygonality } })),
            authenticationToken.MPlus.SessionId))
            .Result;
        _taskRegistry[taskId] = authenticationToken;

        await Bot.TrySendMessageAsync(chatId, "Resulting media file will be sent back to you as soon as it's ready.",
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskQueryFlags.Details, taskId)))
            );
    }
}
