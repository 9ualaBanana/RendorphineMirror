﻿using GIBS.Commands;
using Telegram.MPlus.Security;
using Telegram.StableDiffusion;

namespace Telegram.Commands.Handlers;

public class PromptCommand : CommandHandler
{
    readonly StableDiffusionPrompt _midjourneyPrompt;

    public PromptCommand(
        StableDiffusionPrompt midjourneyPrompt,
        Command.Factory commandFactory,
        Command.Received receivedCommand,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PromptCommand> logger)
        : base(commandFactory, receivedCommand, bot, httpContextAccessor, logger)
    {
        _midjourneyPrompt = midjourneyPrompt;
    }

    public override Command Target => CommandFactory.Create("prompt");

    protected override async Task HandleAsync(Command receivedCommand)
    {
        var prompt = await _midjourneyPrompt.NormalizeAsync(receivedCommand.UnquotedArguments, MPlusIdentity.UserIdOf(User), RequestAborted);
        await _midjourneyPrompt.SendAsync(new(prompt, Message), RequestAborted);
    }
}
