using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Security.Authorization;
using Telegram.Services.Node;
using static Telegram.Security.Authorization.MPlusAuthorizationPolicyBuilder;

namespace Telegram.Commands.Handlers;

public partial class PingCommand
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

        internal override Command Target => CommandFactory.Create("adminping");

        public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder()
            .Add(AccessLevelRequirement.Admin)
            .Build();

        protected override async Task HandleAsync(Command receivedCommand)
        {
            var messageBuilder = new StringBuilder().AppendHeader(Header);

            var nodeNames = receivedCommand.QuotedArguments.Select(nodeName => nodeName.CaseInsensitive()).ToHashSet();
            foreach (var theUserNodes in _userNodes)
                messageBuilder.AppendLine(ListOnlineNodes(theUserNodes.Value, nodeNames).ToString());

            await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
        }
    }
}
