using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NodeCommon.Tasks;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskState
{
    Queued,
    Input,
    Active,
    Output,
    Finished,
    Canceled,
    Failed,
}

public static class TaskStateExtensions
{
    /// <summary> Returns true if task state is either <see cref="TaskState.Finished"/>, <see cref="TaskState.Canceled"/> or <see cref="TaskState.Failed"/> </summary>
    public static bool IsFinished(this TaskState state) => state >= TaskState.Finished;
}