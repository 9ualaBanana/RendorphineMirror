using Microsoft.AspNetCore.Authorization;

namespace Telegram.Security.Authorization;

/// <summary>
/// Allows services to provide class-specific <see cref="IAuthorizationRequirement"/>s that must be met during imperative authorization.
/// </summary>
/// <remarks>
/// Imperative authorization is performed by explicitly using and calling the corresponding methods of <see cref="IAuthorizationService"/>.
/// Declarative authorization. on the other hand, is performed using <see cref="AuthorizeAttribute"/>.
/// </remarks>
internal interface IAuthorizationRequirementsProvider
{
    IEnumerable<IAuthorizationRequirement> Requirements { get; }
}
