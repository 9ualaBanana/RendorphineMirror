using Microsoft.AspNetCore.Mvc;

namespace TrialUsersMediator;

public class Authentication
{
    readonly TrialUsersDbContext _database;
    readonly TrialUser.Identity _trialUserIdentity;

    public Authentication(TrialUsersDbContext database, TrialUser.Identity trialUserIdentity)
    {
        _database = database;
        _trialUserIdentity = trialUserIdentity;
    }

    internal async Task<Authentication.Check> CheckAsync(TrialUser user, string userId)
    {
        if (userId == _trialUserIdentity._.UserId)
            if (await _database.Authenticated(user) is TrialUser.Entity authenticatedTrialUser)
                return Check.Successful(authenticatedTrialUser);
            else
            {
                // TODO: Throw critical exception here.
                // (There is no record for that user in the database (it could be dropped) but it has been authentificated)
                return Check.Failed(new NotFoundObjectResult(user));
            }
        else
        {
            return Check.Failed(new UnauthorizedObjectResult(userId));
        }
    }


    internal class Check
    {
        internal ObjectResult Result;
        internal TrialUser.Entity? AuthenticatedUser;
        /// <remarks>
        /// It's important to note that this property result contains <see cref="TrialUser.Entity"/>
        /// that is explicitly downcast to <see cref="TrialUser"/> by creating a new object of this type
        /// to exclude reference properties of <see cref="TrialUser.Entity"/> when writing the resulting authenticated user to the response
        /// (properties of objects of derived types are still written when downcast is performed by implicit or explicit operator).
        /// </remarks>
        internal ActionResult<TrialUser> AsActionResult
            => AuthenticatedUser is not null ? AuthenticatedUser.Downcast() : Result;

        Check(ObjectResult result, TrialUser.Entity? authenticatedUser)
            => (Result, AuthenticatedUser) = (result, authenticatedUser);

        internal static Check Successful(TrialUser.Entity authenticatedUser)
            => new(new OkObjectResult(default), authenticatedUser);
        internal static Check Failed(ObjectResult result)
            => new(result, null);
    }
}
