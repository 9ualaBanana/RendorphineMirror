using Microsoft.AspNetCore.Authorization;
namespace Telegram.Security.Authorization;

public class MPlusAuthenticationRequirement : IAuthorizationRequirement
{
    internal static MPlusAuthenticationRequirement Instance = new();

	MPlusAuthenticationRequirement()
	{
	}
}
