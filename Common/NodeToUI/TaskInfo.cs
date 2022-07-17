using Newtonsoft.Json.Linq;

namespace Common.NodeToUI;

public record TaskInfo(string UserId, long Registered, TaskObject Object, JObject Input, JObject Output, JObject Data);