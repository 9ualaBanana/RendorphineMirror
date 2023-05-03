using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Security.Authorization;
using Telegram.Services.Node;
using static Telegram.Security.Authorization.MPlusAuthorizationPolicyBuilder;

namespace Telegram.Commands.Handlers;

public partial class PingListCommand
{
    public class Admin : CommandHandler, IAuthorizationPolicyProtected
    {
        readonly UserNodes _userNodes;

        public Admin(
            UserNodes userNodes,
            CommandParser parser,
            TelegramBot bot,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Admin> logger)
            : base(parser, bot, httpContextAccessor, logger)
        {
            _userNodes = userNodes;
        }

        internal override Command Target => "adminpinglist";

        public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder()
            .Add(AccessLevelRequirement.Admin)
            .Build();

        protected override async Task HandleAsync(ParsedCommand receivedCommand)
        {
            var messageBuilder = new StringBuilder().AppendHeader(Header);

            foreach (var theUserNodes in _userNodes)
                messageBuilder.AppendLine(ListNodesOrderedByName(theUserNodes.Value).ToString());

            await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
        }
    }
}
