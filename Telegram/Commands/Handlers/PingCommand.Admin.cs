using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class PingCommand
{
    public class Admin : CommandHandler, IAuthorizationRequirementsProvider
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

        public IEnumerable<IAuthorizationRequirement> Requirements { get; }
            = IAuthorizationRequirementsProvider.Provide(
                MPlusAuthenticationRequirement.Instance,
                AccessLevelRequirement.Admin
                );

        internal override Command Target => "adminping";

        protected override async Task HandleAsync(ParsedCommand receivedCommand)
        {
            var messageBuilder = new StringBuilder().AppendHeader(Header);

            var nodeNames = receivedCommand.QuotedArguments.Select(nodeName => nodeName.CaseInsensitive()).ToHashSet();
            foreach (var theUserNodes in _userNodes)
                messageBuilder.AppendLine(ListOnlineNodes(theUserNodes.Value, nodeNames).ToString());

            await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
        }
    }
}
