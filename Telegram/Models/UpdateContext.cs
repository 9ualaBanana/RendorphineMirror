using Telegram.Bot.Types;
using Telegram.Middleware.UpdateRouting;

namespace Telegram.Models;

// Authentication/Authorization info should be stored here as well and be populated by corresponding middleware.
// It likely should represent M+ user.
internal class UpdateContext
{
    internal readonly Update Update;

    internal UpdateContext(Update update)
    {
        Update = update;
    }

    // Define a separate static class for that functionality.

    /// <summary>
    /// Adds this instance to <see cref="HttpContext.Items"/> which can be retrieved using <see cref="RetrieveFromCacheOf(HttpContext)"/>.
    /// </summary>
    /// <remarks>
    /// Intended to be called once per request by <see cref="UpdateContextConstructorMiddleware"/>.
    /// Following call attempts will throw.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when this method is called more than once per request.</exception>
    internal void CacheInside(HttpContext context) => context.Items.Add(ItemsKey, this);

    /// <summary>
    /// Retrieves <see cref="UpdateContext"/> instance stored inside <see cref="HttpContext.Items"/>
    /// </summary>
    internal static UpdateContext RetrieveFromCacheOf(HttpContext httpContext) => (httpContext.Items[ItemsKey] as UpdateContext)!;

    static readonly object ItemsKey = new();
}
