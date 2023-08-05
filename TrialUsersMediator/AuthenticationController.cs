using Microsoft.AspNetCore.Mvc;
using Node.Tasks.Models;
using Telegram.Infrastructure.Bot;

namespace TrialUsersMediator;

[ApiController]
[Route("authentication")]
public class AuthenticationController : ControllerBase
{
    readonly Authentication _authentication;
    readonly TrialUsersDbContext _database;
    readonly TrialUser.Identity _trialUserIdentity;

    public AuthenticationController(
        Authentication authentication,
        TrialUsersDbContext database,
        TrialUser.Identity trialUserIdentity)
    {
        _authentication = authentication;
        _database = database;
        _trialUserIdentity = trialUserIdentity;
    }

    [HttpGet("telegram_user")]
    public async Task<string> Authenticate(
        [FromQuery] long chatId,
        [FromQuery] TelegramBot.User.LoginWidgetData telegramLoginWidgetData)
    {
        try { await AuthenticateAsync(); }
        catch { /* Already authenticated */ }

        return _trialUserIdentity._.SessionId;


        async Task AuthenticateAsync()
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

    [HttpGet("check")]
    public async Task<ActionResult<TrialUser>> Check(
        [FromQuery] TrialUser user,
        [FromQuery] string userId)
        => (await _authentication.CheckAsync(user, userId)).AsActionResult;
}