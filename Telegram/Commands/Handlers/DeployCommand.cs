using Microsoft.AspNetCore.Authorization;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.Commands;
using Telegram.MPlus.Security;
using Telegram.Security.Authorization;
using Telegram.Services.Node;

namespace Telegram.Commands.Handlers;

public class DeployCommand : CommandHandler, IAuthorizationPolicyProtected
{
    readonly UserNodes _userNodes;
    readonly HttpClient _httpClient;

    public DeployCommand(
        UserNodes userNodes,
        IHttpClientFactory httpClientFactory,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeployCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _userNodes = userNodes;
        _httpClient = httpClientFactory.CreateClient();
    }

    internal override Command Target => CommandFactory.Create("deploy");

    public AuthorizationPolicy AuthorizationPolicy { get; } = new MPlusAuthorizationPolicyBuilder().Build();

    protected override async Task HandleAsync(Command receivedCommand)
    {
        var pluginTypes = receivedCommand.UnquotedArguments.Select(type => Enum.Parse<PluginType>(type, true));
        var nodeNames = receivedCommand.QuotedArguments;

        var plugins = pluginTypes
            .Select(type => new PluginToDeploy(type, string.Empty)).ToHashSet();

        var api = new Apis(new Api(_httpClient), MPlusIdentity.SessionIdOf(User), default);
        var userSettings = await api.GetSettingsAsync().GetValueOrDefault();
        if (userSettings is null)
        { await Bot.SendMessageAsync_(ChatId, "Plugins couldn't be deployed."); return; }
        if (nodeNames.Any())
        {
            if (!_userNodes.TryGetUserNodeSupervisor(MPlusIdentity.UserIdOf(User), out var userNodesSupervisor, Bot, ChatId))
                return;

            foreach (var node in nodeNames.SelectMany(userNodesSupervisor.GetNodesByName))
            {
                foreach (var plugin in plugins)
                    userSettings.Install(node.Guid, plugin.Type, plugin.Version);

                if (!await api.SetSettingsAsync(userSettings))
                { await Bot.SendMessageAsync_(ChatId, "Plugins couldn't be deployed."); return; }
            }
        }
        else
        {
            foreach (var plugin in plugins)
                userSettings.Install(plugin.Type, plugin.Version);

            if (!await api.SetSettingsAsync(userSettings))
            { await Bot.SendMessageAsync_(ChatId, "Plugins couldn't be deployed."); return; }
        }
        await Bot.SendMessageAsync_(ChatId, "Plugins successfully added to the deploy queue.");
    }
}
