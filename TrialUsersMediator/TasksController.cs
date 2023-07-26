using Microsoft.AspNetCore.Mvc;
using Node.Tasks.Models;

namespace TrialUsersMediator;

[ApiController]
public class TasksController : ControllerBase
{
    readonly Authentication _authentication;
    readonly TrialUsersDbContext _database;

    public TasksController(
        Authentication authentication,
        TrialUsersDbContext database)
    {
        _authentication = authentication;
        _database = database;
    }

    [HttpGet("try_reduce_quota")]
    public async Task<IActionResult> TryReduceQuota(
        [FromQuery] string taskAction,
        [FromQuery] TrialUser user,
        [FromQuery] string userId)
    {
        var authenticationCheck = await _authentication.CheckAsync(user, userId);
        if (authenticationCheck.AuthenticatedUser is TrialUser.Entity authenticatedTrialUser)
        {
            var quotaManager = TrialUser.Quota<TaskAction>.Manager.For(authenticatedTrialUser, Enum.Parse<TaskAction>(taskAction));
            if (quotaManager.Value > 0)
            {
                quotaManager.Decrease();
                // Quota.Entity must be updated manually (not by EF Core following the reference property of TrialUser.Entity when detecting changes)
                // because changes to the dictionary are not detected otherwise.
                _database.Update(authenticatedTrialUser.Quota_);
                _database.SaveChanges();
            }
            else return Conflict();
        }
        
        return authenticationCheck.Result;
    }
}