using Newtonsoft.Json.Linq;

namespace Common.Tasks;

public record TaskFullState(TaskState State, double Progress, TaskServer Server, JObject Output);
public record TaskServer(string Host, string Userid, string Nickname);
