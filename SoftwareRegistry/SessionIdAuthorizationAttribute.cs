using Microsoft.AspNetCore.Mvc.Filters;

namespace SoftwareRegistry;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionIdAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any()) return;

        var sid = context.HttpContext.Request.Query["sessionid"];
        if (sid == "63fe288368974192c27a5388") return;

        context.Result = new NotFoundResult();
    }
}