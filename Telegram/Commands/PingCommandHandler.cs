using Telegram.Models;
using Telegram.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Telegram.Commands.SyntaxAnalysis;
using Telegram.Bot;

namespace Telegram.Commands;

public class PingCommandHandler : CommandHandler, IAuthorizationRequirementsProvider
{
    private readonly TelegramBot bot;
    readonly ILogger<PingCommandHandler> _logger;

    public PingCommandHandler(Bot.TelegramBot bot, CommandParser parser, ILogger<PingCommandHandler> logger)
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

    protected override async Task HandleAsync(HttpContext context, ParsedCommand parsedCommand, CancellationToken cancellationToken)
    {
        await bot.SendMessageAsync_(context.GetUpdate().Message.Chat.Id, $"Command: {parsedCommand.Command}\nUnquotedArguments: {string.Join(", ", parsedCommand.UnquotedArguments)}\nQuotedArguments: {string.Join(", ", parsedCommand.QuotedArguments)}");
    }
}
