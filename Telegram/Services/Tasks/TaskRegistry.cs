using Telegram.Services.Telegram.Authentication;

namespace Telegram.Services.Tasks;

public class TaskRegistry : Dictionary<string, ChatAuthenticationToken>
{
}
