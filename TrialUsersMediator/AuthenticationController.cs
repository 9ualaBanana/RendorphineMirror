using Microsoft.AspNetCore.Mvc;
using Node.Tasks.Models;
using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator;

[ApiController]
[Route("authenticate")]
public class AuthenticationController : ControllerBase
{
    readonly TrialUsersDbContext _database;
    readonly TrialUser.Identity _trialUserIdentity;

    public AuthenticationController(TrialUsersDbContext database, TrialUser.Identity trialUserIdentity)
    {
        _database = database;
        _trialUserIdentity = trialUserIdentity;
    }

    [HttpGet("telegram_user")]
    public async Task<string> Authenticate(
        [FromQuery] long chatId,
        [FromQuery] TelegramBot.User.LoginWidgetData telegramLoginWidgetData)
    {
        try
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
        catch { /* Already authenticated */ }

        return _trialUserIdentity._.SessionId;
    }
}