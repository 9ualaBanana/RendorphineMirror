using Microsoft.Extensions.Caching.Memory;
using Telegram.MPlus.Clients;

namespace Telegram.StableDiffusion;

public class StableDiffusionPrompt
{
    readonly StockSubmitterClient _stockSubmitterClient;
    readonly CachedMessages _sentPromptMessages;

    public StableDiffusionPrompt(StockSubmitterClient stockSubmitterClient, CachedMessages sentPromptMessages)
    {
        _stockSubmitterClient = stockSubmitterClient;
        _sentPromptMessages = sentPromptMessages;
    }

    internal async Task<string> NormalizeAsync(IEnumerable<string> promptTokens, string userId, CancellationToken cancellationToken)
    {
        // Telegram converts "--" (double en-dash) to "—" (single em-dash);
        const char EmDash = '—';
        const string DoubleEnDash = "--";

        var imagePrompts = promptTokens.Where(token => Uri.IsWellFormedUriString(token, UriKind.Absolute));
        var textPrompts = promptTokens.Where(token => !Uri.IsWellFormedUriString(token, UriKind.Absolute) && !token.StartsWith(EmDash) && !token.StartsWith(DoubleEnDash));
        var translatedTextPrompts = await _stockSubmitterClient.TranslateAsync(textPrompts, userId, cancellationToken);
        var parameters = promptTokens.Where(token => token.StartsWith(EmDash) || token.StartsWith(DoubleEnDash));

        var normalizedPromptTokens = new List<string>();
        normalizedPromptTokens.AddRange(imagePrompts);
        normalizedPromptTokens.AddRange(translatedTextPrompts);
        normalizedPromptTokens.AddRange(parameters);

        return string.Join(' ', promptTokens);
    }

    internal async Task SendAsync(StableDiffusionPromptMessage promptMessage, CancellationToken cancellationToken)
    {
        var promptId = Guid.NewGuid();

        // Send Prompt() to the Renderphine.

        _sentPromptMessages.Add(promptId, promptMessage);
    }


    public class CachedMessages
    {
        readonly IMemoryCache _cache;

        public CachedMessages(IMemoryCache cache)
        {
            _cache = cache;
        }

        internal void Add(Guid promptId, StableDiffusionPromptMessage promptMessage)
            => _cache.CreateEntry(promptId).SetValue(promptMessage)
            .SetSlidingExpiration(TimeSpan.FromMinutes(30))
            .SetAbsoluteExpiration(TimeSpan.FromDays(1))
            .Dispose();

        internal StableDiffusionPromptMessage? TryRetrieveBy(Guid promptId)
            => _cache.TryGetValue(promptId, out StableDiffusionPromptMessage promptMessage) ?
            promptMessage : null;
    }
}
