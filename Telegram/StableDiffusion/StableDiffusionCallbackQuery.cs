using Telegram.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;

namespace Telegram.StableDiffusion;

public class StableDiffusionCallbackQueryHandler : CallbackQueryHandler<StableDiffusionCallbackQuery, StableDiffusionCallbackData>
{
    readonly StableDiffusionPrompt _midjourneyPromptManager;
    readonly StableDiffusionPrompt.CachedMessages _cachedPromptMessages;
    readonly CommandParser _commandParser;

    public StableDiffusionCallbackQueryHandler(
        StableDiffusionPrompt midjourneyPromptManager,
        StableDiffusionPrompt.CachedMessages cachedPromptMessages,
        CommandParser commandParser,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<StableDiffusionCallbackQueryHandler> logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        _midjourneyPromptManager = midjourneyPromptManager;
        _cachedPromptMessages = cachedPromptMessages;
        _commandParser = commandParser;
    }

    public override async Task HandleAsync(StableDiffusionCallbackQuery callbackQuery)
    {
        if (callbackQuery.Data is StableDiffusionCallbackData.Regenerate)
            await RegenerateAsync();


        async Task RegenerateAsync()
        {
            if (_cachedPromptMessages.TryRetrieveBy(callbackQuery.PromptId) is StableDiffusionPromptMessage promptMessage)
                await _midjourneyPromptManager.SendAsync(promptMessage, RequestAborted);
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
