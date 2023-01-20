using Telegram.Middleware.UpdateRouting;

namespace Telegram.Models;

/// <summary>
/// Service for storing/retrieving <see cref="UpdateContext"/> instance inside/from <see cref="HttpContext.Items"/>.
/// </summary>
/// <remarks>
/// Must be used only in contexts where current <see cref="Microsoft.AspNetCore.Http.HttpContext"/> instance
/// can be accessed via <see cref="IHttpContextAccessor"/>.
/// </remarks>
public class UpdateContextCache
{
    readonly ILogger _logger;

	readonly IHttpContextAccessor _httpContextAccessor;

	public UpdateContextCache(IHttpContextAccessor httpContextAccessor, ILogger<UpdateContextCache> logger)
	{
		_httpContextAccessor = httpContextAccessor;
        _logger = logger;
	}

    /// <summary>
    /// Caches <paramref name="updateContext"/> inside <see cref="HttpContext.Items"/>
    /// from where it can be retrieved using <see cref="Retrieve"/>.
    /// </summary>
    /// <remarks>
    /// Intended to be called once per request by <see cref="UpdateContextConstructorMiddleware"/>.
    /// Following call attempts will result in an exception.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when this method is called more than once per request.</exception>
    internal void Cache(UpdateContext updateContext) => _httpContextAccessor.HttpContext?.Items.Add(CacheKey, updateContext);

    /// <summary>
    /// Retrieves <see cref="UpdateContext"/> instance that is cached inside <see cref="HttpContext.Items"/>.
    /// </summary>
    /// <remarks>
    /// Trying to <see cref="Retrieve"/> <see cref="UpdateContext"/> before it was cached by <see cref="Cache"/> results in an exception.
    /// </remarks>
    internal UpdateContext Retrieve() => (HttpContext.Items[CacheKey] as UpdateContext)!;

    static readonly object CacheKey = new();

    HttpContext HttpContext
    {
        get
        {
            if (_httpContextAccessor.HttpContext is HttpContext httpContext) return httpContext;
            else
            {
                const string errorMessage = $"{nameof(HttpContext)} instance is inaccessible from the current context";
                var exception = new MemberAccessException(errorMessage);
                _logger.LogCritical(exception, message: default);
                throw exception;
            }
        }
    }
}
