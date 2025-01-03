﻿using Telegram.Bot.Types;
using Telegram.Services.Telegram.Updates.Commands;

namespace Telegram.Telegram.Authentication.Models;

internal record TelegramCredentials(string Login, string Password, ChatId ChatId)
{
    internal static bool TryParse(Message message, out TelegramCredentials? credentials)
    {
        try { credentials = Parse(message); return true; }
        catch (Exception) { credentials = null; return false; }
    }
    internal static TelegramCredentials Parse(Message message)
    {
        ChatId id = message.Chat.Id;
        var messageParts = message.Text!.Arguments().ToArray();
        return new(messageParts[0], messageParts[1], id);
    }
}
