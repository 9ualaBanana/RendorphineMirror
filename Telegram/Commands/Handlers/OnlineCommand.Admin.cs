using Microsoft.AspNetCore.Authorization;
using Telegram.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.Infrastructure.Commands.SyntacticAnalysis;
using Telegram.Security.Authorization;
using Telegram.Services.Node;
using static Telegram.Security.Authorization.MPlusAuthorizationPolicyBuilder;

namespace Telegram.Commands.Handlers;

public partial class OnlineCommand
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

        internal override Command Target => "adminonline";

        protected override async Task HandleAsync(ParsedCommand receivedCommand)
        {
            var nodesOnline = _userNodes.Aggregate(0, (nodesOnline, theUserNodes) => nodesOnline += theUserNodes.Value.NodesOnline.Count);
            var nodesOffline = _userNodes.Aggregate(0, (nodesOffline, theUserNodes) => nodesOffline += theUserNodes.Value.NodesOffline.Count);

            var message = BuildMessage(nodesOnline, nodesOffline);

            await Bot.SendMessageAsync_(ChatId, message);
        }
    }
}
