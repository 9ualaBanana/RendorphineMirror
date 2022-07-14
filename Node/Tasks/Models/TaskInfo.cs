using Newtonsoft.Json.Linq;

namespace Node.Tasks.Models;

public record TaskInfo(
    string UserId,
    long Registered,
    TaskObject Object,
    JObject Input,
    JObject Output,
    JObject Data,
    TaskState State,
    int Progress,
    string Origin,
    ServerInfo? Server)
{
    public ITaskInputInfo DeserializeInput()
    {
        var type = Enum.Parse<TaskInputOutputType>(Input["type"]!.Value<string>()!);

        if (type == TaskInputOutputType.MPlus)
            return Input.ToObject<MPlusTaskInputInfo>()!;

        throw new NotSupportedException($"Task input type {type} is not supported");
    }
    public ITaskOutputInfo DeserializeOutput()
    {
        var type = Enum.Parse<TaskInputOutputType>(Output["type"]!.Value<string>()!);

        if (type == TaskInputOutputType.MPlus)
            return Output.ToObject<MPlusTaskOutputInfo>()!;

        throw new NotSupportedException($"Task output type {type} is not supported");
    }
}
