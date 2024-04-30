using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Node.Listeners;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionIdAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any()) return;
        if (await CheckAuthentication(context)) return;

        context.Result = new NotFoundResult();
    }

    static async Task<bool> CheckAuthentication(AuthorizationFilterContext context)
    {
        var sid = context.HttpContext.Request.Query["sessionid"].ToString()
            ?? context.HttpContext.Request.Cookies["sessionid"];

        return sid is not null && await CheckAuthentication(sid);
    }
    static async Task<bool> CheckAuthentication(string sid)
    {
        var oursid = Settings.SessionId;
        if (sid == oursid) return true;

        var nodes = await new Apis(Api.Default, sid).GetMyNodesAsync().ConfigureAwait(false);
        if (!nodes) return false;

        var theiruserid = nodes.Result.Select(x => x.UserId).FirstOrDefault();
        if (theiruserid is null) return false;

        var myuserid = Settings.UserId;
        return myuserid == theiruserid;
    }
}
