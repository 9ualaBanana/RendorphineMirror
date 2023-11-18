﻿using GIBS.Commands;
using Telegram.MPlus.Security;
using Telegram.StableDiffusion;

namespace Telegram.Commands.Handlers;

public class PromptCommand : CommandHandler
{
    readonly StableDiffusionPrompt _stableDiffusionPrompt;

    public PromptCommand(
        StableDiffusionPrompt midjourneyPrompt,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PromptCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _stableDiffusionPrompt = midjourneyPrompt;
    }

    public override Command Target => CommandFactory.Create("prompt");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        await _stableDiffusionPrompt.SendAsync(receivedCommand.UnquotedArguments, Message, User.ToTelegramBotUserWith(ChatId), RequestAborted);
    }
}
