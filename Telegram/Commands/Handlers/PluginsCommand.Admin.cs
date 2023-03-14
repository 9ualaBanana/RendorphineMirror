using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public partial class PluginsCommand
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

        internal override Command Target => "adminplugins";

        protected override async Task HandleAsync(ParsedCommand receivedCommand, HttpContext context)
        {
            var messageBuilder = new StringBuilder();

            var nodeNamesWhosePluginsToShow = receivedCommand.QuotedArguments;
            foreach (var theUserNodes in _userNodes)
                messageBuilder.AppendLine(
                    ListInstalledPluginsFor(nodeNamesWhosePluginsToShow, theUserNodes.Value).ToString());

            if (messageBuilder.Length == 0) messageBuilder.AppendLine("No plugins are installed on the specified nodes.");

            await Bot.SendMessageAsync_(Update.Message!.Chat.Id, messageBuilder.ToString());
        }
    }
}
