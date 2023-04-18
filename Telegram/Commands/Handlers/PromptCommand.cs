using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.MPlus;
using Telegram.StableDiffusion;

namespace Telegram.Commands.Handlers;

public class PromptCommand : CommandHandler
{
    readonly StableDiffusionPrompt _midjourneyPrompt;

    public PromptCommand(
        StableDiffusionPrompt midjourneyPrompt,
        CommandParser parser,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PromptCommand> logger)
        : base(parser, bot, httpContextAccessor, logger)
    {
        _midjourneyPrompt = midjourneyPrompt;
    }

    internal override Command Target => "prompt";

    protected override async Task HandleAsync(ParsedCommand receivedCommand)
    {
        var prompt = await _midjourneyPrompt.NormalizeAsync(receivedCommand.UnquotedArguments, MPlusIdentity.UserIdOf(User), RequestAborted);
        await _midjourneyPrompt.SendAsync(new(prompt, Message), RequestAborted);
    }
}
