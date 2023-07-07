using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodeCommon.Tasks;

namespace TrialUsersMediator;

[ApiController]
public class TrialUsersController : ControllerBase
{
    readonly TrialUsersDbContext _database;
    readonly TrialUser.Identity _trialUserIdentity;

    public TrialUsersController(TrialUsersDbContext database, TrialUser.Identity trialUserIdentity)
    {
        _database = database;
        _trialUserIdentity = trialUserIdentity;
    }

    [HttpGet("try_reduce_quota")]
    public async Task<ActionResult<object>> TryReduceQuota(
        [FromQuery] string taskAction,
        [FromQuery] TrialUser user,
        string userId)
    {
        if (userId == _trialUserIdentity._.UserId)
            if (await _database.AuthenticatedUsers.SingleOrDefaultAsync(trialUser => trialUser == new TrialUser.Entity(user)) is TrialUser.Entity trialUser)
            {
                TrialUser.Quota<TaskAction>.Manager.For(trialUser, Enum.Parse<TaskAction>(taskAction)).Decrease();
                _database.Update(trialUser);
                _database.SaveChanges();
                return Ok();
            }
            else
            {
                // TODO: Throw critical exception here.
                return NotFound();
            }
        else
        {
            return Unauthorized();
        }
    }
}