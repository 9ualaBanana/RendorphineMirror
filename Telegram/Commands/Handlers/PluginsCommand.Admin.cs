using GIBS.Authorization;
using GIBS.Commands;
using Microsoft.AspNetCore.Authorization;
using System.Text;
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
            Command.Factory commandFactory,
            Command.Received receivedCommand,
            TelegramBot bot,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Admin> logger)
            : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
        {
            _userNodes = userNodes;
        }

        public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder()
            .Add(AccessLevelRequirement.Admin)
            .Build();

        public override Command Target => CommandFactory.Create("adminplugins");

        protected override async Task HandleAsync(Command receivedCommand)
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
