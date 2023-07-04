using Microsoft.AspNetCore.Mvc;
using NodeCommon.Tasks;
using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator;

[ApiController]
[Route("authenticate")]
public class AuthenticationController : ControllerBase
{
    readonly TrialUsersDbContext _database;

    public AuthenticationController(TrialUsersDbContext database)
    {
        _database = database;
    }

    [HttpGet("telegram_user")]
    public async Task Authenticate(
        [FromQuery] long chatId,
        [FromQuery] TelegramBot.User.LoginWidgetData telegramLoginWidgetData)
    {
        var trialUser = new TrialUser() { Identifier = chatId, Platform = Platform.Telegram };
        var quota = TrialUser.Quota<TaskAction>.Default;

        _database.Add(new TrialUser.Entity(trialUser)
        {
            Info_ = TrialUser.Info.Entity.From(telegramLoginWidgetData),
            Quota_ = new TrialUser.Quota<TaskAction>.Entity(quota)
        });
        await _database.SaveChangesAsync();
    }
}