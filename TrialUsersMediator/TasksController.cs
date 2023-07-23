﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Node.Tasks.Models;

namespace TrialUsersMediator;

[ApiController]
public class TasksController : ControllerBase
{
    readonly TrialUsersDbContext _database;
    readonly TrialUser.Identity _trialUserIdentity;

    public TasksController(TrialUsersDbContext database, TrialUser.Identity trialUserIdentity)
    {
        _database = database;
        _trialUserIdentity = trialUserIdentity;
    }

    [HttpGet("try_reduce_quota")]
    public async Task<IActionResult> TryReduceQuota(
        [FromQuery] string taskAction,
        [FromQuery] TrialUser user,
        string userId)
    {
        if (userId == _trialUserIdentity._.UserId)
            if (await User() is TrialUser.Entity authenticatedTrialUser)
            {
                TrialUser.Quota<TaskAction>.Manager.For(authenticatedTrialUser, Enum.Parse<TaskAction>(taskAction)).Decrease();
                // Quota.Entity must be updated manually (not by EF Core following the reference property of TrialUser.Entity when detecting changes)
                // because changes to the dictionary are not detected otherwise.
                _database.Update(authenticatedTrialUser.Quota_);
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


        async Task<TrialUser.Entity?> User()
            => await _database.AuthenticatedUsers.SingleOrDefaultAsync(trialUser => trialUser == new TrialUser.Entity(user));
    }
}