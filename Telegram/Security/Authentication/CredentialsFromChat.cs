using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security;
using Telegram.Bot.Types;
using Telegram.Commands;

namespace Telegram.Security.Authentication;

/// <summary>
/// Credentials received by means of Telegram <see cref="Message"/>.
/// </summary>
/// <remarks>
/// Credentials must be provided in the format <c>`login password`</c>.
/// </remarks>
/// <param name="ChatId">The <see cref="Bot.Types.ChatId"/> from where the message containing credentials was sent.</param>
internal class CredentialsFromChat : NetworkCredential
{
    /// <summary>
    /// The <see cref="Bot.Types.ChatId"/> from where the message containing credentials was sent.
    /// </summary>
    internal readonly ChatId ChatId;

    CredentialsFromChat(string userName, string password, ChatId chatId, string? domain = null)
        : base(userName, password, domain)
    {
        ChatId = chatId;
    }

    CredentialsFromChat(string userName, SecureString password, ChatId chatId, string? domain = null)
        : base(userName, password, domain)
    {
        ChatId = chatId;
    }

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
    internal static bool TryParse(Message message, [NotNullWhen(true)] out CredentialsFromChat? credentials, bool messageIsCommand = true)
    {
        try { credentials = Parse(message); return true; }
        catch (Exception) { credentials = null; return false; }
    }

    static CredentialsFromChat Parse(Message message, bool messageIsCommand = true)
    {
        ChatId id = message.Chat.Id;
        var messageParts = message.Text!.Arguments().ToArray();
        var (userName, password) = messageIsCommand ? (messageParts[1], messageParts[2]) : (messageParts[0], messageParts[1]);
        return new(userName, password, id);
    }
}
