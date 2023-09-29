using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.CallbackQueries.Serialization;

namespace Telegram.StableDiffusion;

public class StableDiffusionCallbackQueryHandler : CallbackQueryHandler<StableDiffusionCallbackQuery, StableDiffusionCallbackData>
{
    readonly StableDiffusionPrompt _stableDiffusionPrompt;
    readonly StableDiffusionPrompt.CachedMessages _cachedPromptMessages;

    public StableDiffusionCallbackQueryHandler(
        StableDiffusionPrompt stableDiffusionPrompt,
        StableDiffusionPrompt.CachedMessages cachedPromptMessages,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<StableDiffusionCallbackQueryHandler> logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        _stableDiffusionPrompt = stableDiffusionPrompt;
        _cachedPromptMessages = cachedPromptMessages;
    }

    public override async Task HandleAsync(StableDiffusionCallbackQuery callbackQuery)
    {
        if (callbackQuery.Data is StableDiffusionCallbackData.Regenerate)
            await RegenerateAsync();


        async Task RegenerateAsync()
        {
            if (_cachedPromptMessages.TryRetrieveBy(callbackQuery.PromptId) is StableDiffusionPromptMessage promptMessage)
                await _stableDiffusionPrompt.SendAsync(promptMessage, User.ToTelegramBotUserWith(ChatId), RequestAborted);
        }
    }
}

public record StableDiffusionCallbackQuery : CallbackQuery<StableDiffusionCallbackData>
{
    internal Guid PromptId => Guid.Parse(ArgumentAt(0).ToString()!);
}

public enum StableDiffusionCallbackData
{
    Regenerate
}
