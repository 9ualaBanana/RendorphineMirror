using Telegram.Models;
using Telegram.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Telegram.Commands.SyntaxAnalysis;
using Telegram.Bot;

namespace Telegram.Commands;

public class PingCommand : CommandHandler, IAuthorizationRequirementsProvider
{
    private readonly TelegramBot bot;
    readonly ILogger<PingCommand> _logger;

    public PingCommand(Bot.TelegramBot bot, CommandParser parser, ILogger<PingCommand> logger)
        : base(parser, logger)
    {
        this.bot = bot;
        _logger = logger;
    }

    public IEnumerable<IAuthorizationRequirement> Requirements => new IAuthorizationRequirement[]
    {
        //MPlusAuthenticationRequirement.Instance,
        //new AccessLevelRequirement(AccessLevel.User)
    };

    internal override Command Target => "/ping";

    protected override async Task HandleAsync(UpdateContext updateContext, ParsedCommand parsedCommand, CancellationToken cancellationToken)
    {
        bot.SendMessageAsync_(updateContext.Update.Message.Chat.Id, $"Command: {parsedCommand.Command}\nUnquotedArguments: {string.Join(", ", parsedCommand.UnquotedArguments)}\nQuotedArguments: {string.Join(", ", parsedCommand.QuotedArguments)}");
    }
}
