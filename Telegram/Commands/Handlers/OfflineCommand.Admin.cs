using Microsoft.AspNetCore.Authorization;
using System.Text;
using Telegram.Infrastructure.Authorization;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Security.Authorization;
using Telegram.Services.Node;
using static Telegram.Security.Authorization.MPlusAuthorizationPolicyBuilder;

namespace Telegram.Commands.Handlers;

public partial class OfflineCommand
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

        public override Command Target => CommandFactory.Create("adminoffline");

        protected override async Task HandleAsync(Command receivedCommand)
        {
            var messageBuilder = new StringBuilder().AppendHeader(Header);

            foreach (var theUserNodes in _userNodes)
                messageBuilder.AppendLine(ListOfflineNodes(theUserNodes.Value).ToString());

            await Bot.SendMessageAsync_(ChatId, messageBuilder.ToString());
        }
    }
}
