using Telegram.Telegram.Authentication.Models;

namespace Telegram.Tasks;

public class RegisteredTasksCache : Dictionary<string, ChatAuthenticationToken>
{
}
