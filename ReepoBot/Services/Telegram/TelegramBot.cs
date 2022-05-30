using ReepoBot.Models;
using Telegram.Bot;

namespace ReepoBot.Services.Telegram;

public class TelegramBot : TelegramBotClient
{
    public Subscriptions Subscriptions = new("subscriptions.txt");

    public TelegramBot(string token)
        : base(token)
    {
    }
}
