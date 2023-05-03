using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Security.Authorization;
using Telegram.Services.Node;
using static Telegram.Security.Authorization.MPlusAuthorizationPolicyBuilder;

namespace Telegram.Commands.Handlers;

public partial class PluginsCommand
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

        public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder()
            .Add(AccessLevelRequirement.Admin)
            .Build();

        internal override Command Target => "adminplugins";

        protected override async Task HandleAsync(ParsedCommand receivedCommand)
        {
            var messageBuilder = new StringBuilder();

            var nodeNamesWhosePluginsToShow = receivedCommand.QuotedArguments;
            foreach (var theUserNodes in _userNodes)
                messageBuilder.AppendLine(
                    ListInstalledPluginsFor(nodeNamesWhosePluginsToShow, theUserNodes.Value).ToString());

            if (messageBuilder.Length == 0) messageBuilder.AppendLine("No plugins are installed on the specified nodes.");

            await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
        }
    }
}
