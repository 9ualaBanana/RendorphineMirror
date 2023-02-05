﻿using Telegram.Bot.Types;
﻿using System.Diagnostics.CodeAnalysis;
using Telegram.Commands;

namespace Telegram.Telegram.Authentication.Models;

/// <summary>
/// Credentials received by means of Telegram <see cref="Message"/>.
/// </summary>
/// <remarks>
/// Credentials must be provided in the format <c>`login password`</c>.
/// </remarks>
/// <param name="ChatId">The <see cref="Bot.Types.ChatId"/> from where the message containing credentials was sent.</param>
internal record TelegramCredentials(string Login, string Password, ChatId ChatId)
{
    /// <summary>
    /// Parses <see cref="CredentialsFromChat"/> from the <paramref name="message"/> if they are in the right format.
    /// </summary>
    /// <param name="message">The <see cref="Message"/> containing credentials.</param>
    /// <param name="credentials">
    /// The <see cref="CredentialsFromChat"/> parsed from the <paramref name="message"/>
    /// if the <paramref name="message"/> contained them in the format <c>`login password`</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if credentials provided inside the <paramref name="message"/>
    /// are in the rigth format; <see langword="false"/> otherwise.
    /// </returns>
    internal static bool TryParse(Message message, [NotNullWhen(true)] out TelegramCredentials? credentials)
    {
        try { credentials = Parse(message); return true; }
        catch (Exception) { credentials = null; return false; }
    }

    static TelegramCredentials Parse(Message message)
    {
        ChatId id = message.Chat.Id;
        var messageParts = message.Text!.Arguments().ToArray();
        return new(messageParts[0], messageParts[1], id);
    }
}
