using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Models;
using Telegram.Security.Authorization;
using Telegram.Security;
using Telegram.Services.Node;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Commands;
using Microsoft.AspNetCore.Authorization;
using Telegram.Commands.Tokenization;

namespace Telegram.Commands;

public class PingCommand : CommandHandler, IAuthorizationRequirementsProvider
{
    readonly CommandTokenizer _tokenizer;
    readonly ILogger<PingCommand> _logger;

    public PingCommand(CommandTokenizer tokenizer, ILogger<PingCommand> logger) : base(tokenizer)
    {
        _tokenizer = tokenizer;
        _logger = logger;
    }

    public IEnumerable<IAuthorizationRequirement> Requirements => new IAuthorizationRequirement[]
    {
        //MPlusAuthenticationRequirement.Instance,
        //new AccessLevelRequirement(AccessLevel.User)
    };

    internal override Command Target => "/ping";

    public override async Task HandleAsync(UpdateContext updateContext, CancellationToken cancellationToken)
    {
        var tokens = _tokenizer.Tokenize(updateContext.Update.Message!.Text!);
        foreach (var token in tokens)
        {
            Console.WriteLine($"Token: {token.GetType()}\nValue: {token.Value}\nLexeme: {token.Lexeme}\n");
        }
    }
}
