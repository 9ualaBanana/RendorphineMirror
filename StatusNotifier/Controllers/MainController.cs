using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StatusNotifier.Controllers;

[ApiController]
public class MainController : ControllerBase
{
    readonly ImmutableArray<long> botTargets = ImmutableArray.Create<long>(
        466253221 // i3ym
    );

    readonly TelegramBotClient BotClient;

    public MainController(TelegramBotClient botClient) => BotClient = botClient;

    [HttpPost("/notify")]
    public async Task<string> Notify(
        [FromForm] string text
    )
    {
        foreach (var target in botTargets)
        {
            await BotClient.SendTextMessageAsync(new ChatId(target), text, parseMode: ParseMode.Markdown);
        }

        return "{ \"ok\": 1 }";
    }
}
