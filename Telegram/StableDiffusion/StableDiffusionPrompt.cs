using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Tasks;
using Telegram.MPlus.Clients;
using Telegram.MPlus.Security;

namespace Telegram.StableDiffusion;

public class StableDiffusionPrompt
{
    readonly BotRTask _botRTask;
    readonly StockSubmitterClient _stockSubmitterClient;
    readonly CachedMessages _sentPromptMessages;
    readonly Uri _hostUrl;

    public StableDiffusionPrompt(BotRTask botRTask, StockSubmitterClient stockSubmitterClient, CachedMessages sentPromptMessages, IOptions<TelegramBot.Options> botOptions)
    {
        _botRTask = botRTask;
        _stockSubmitterClient = stockSubmitterClient;
        _sentPromptMessages = sentPromptMessages;
        _hostUrl = botOptions.Value.Host;
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

    internal async Task SendAsync(IEnumerable<string> promptTokens, Message promptMessage, TelegramBot.User user, CancellationToken cancellationToken)
        => await SendAsync(new(await NormalizeAsync(promptTokens, MPlusIdentity.UserIdOf(user), cancellationToken), promptMessage), user);
    /// <remarks>
    /// Also requires <see cref="User"/> and <see cref="ChatId"/>.
    /// </remarks>
    internal async Task SendAsync(StableDiffusionPromptMessage prompt, TelegramBot.User user)
    {
        var promptId = Guid.NewGuid();

        await _botRTask.TryRegisterAsync(
            new TaskCreationInfo(
                TaskAction.GenerateImageByPrompt,
                new StubTaskInfo(),
                new MPlusTaskOutputInfo(promptId.ToString(), "stablediffusion") { CustomHost = _hostUrl.ToString() },
                new GenerateImageByPromptInfo(ImageGenerationSource.StableDiffusion, prompt.Prompt),
                new TaskObject(promptId.ToString(), default)),
            user
            );

        _sentPromptMessages.Add(promptId, prompt);
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
        { _cache.TryGetValue(promptId, out StableDiffusionPromptMessage? promptMessage); return promptMessage; }
    }
}
