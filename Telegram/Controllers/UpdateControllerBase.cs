using Microsoft.AspNetCore.Mvc;
using Telegram.Models;
using Telegram.Middleware.UpdateRouting;
using System.Security.Claims;

namespace Telegram.Controllers;

/// <summary>
/// A base class for an MVC controller that deals with <see cref="Models.UpdateContext"/> instances
/// constructed by <see cref="UpdateContextConstructorMiddleware"/>.
/// </summary>
public abstract class UpdateControllerBase : ControllerBase
{
	/// <summary>
	/// Gets the <see cref="Models.UpdateContext"/> for the executing action.
	/// </summary>
	public UpdateContext UpdateContext => UpdateContextCache.Retrieve();

	UpdateContextCache UpdateContextCache
		=> _updateContextCache ??= HttpContext.RequestServices.GetRequiredService<UpdateContextCache>();
	UpdateContextCache? _updateContextCache;

	/// <summary>
	/// Gets the user for this request.
	/// </summary>
	public new ClaimsPrincipal User => UpdateContext.User;
}
