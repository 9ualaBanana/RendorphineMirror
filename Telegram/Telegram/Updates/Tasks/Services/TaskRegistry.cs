using Telegram.Telegram.Authentication.Models;

namespace Telegram.Telegram.Updates.Tasks.Services;

public class TaskRegistry : Dictionary<string, ChatAuthenticationToken>
{
}
