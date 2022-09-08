using Newtonsoft.Json;

namespace Common.Tasks;

public record TaskTimes(DateTimeOffset? Input = null, DateTimeOffset? Active = null, DateTimeOffset? Output = null)
{

    [JsonConstructor]
    public TaskTimes(long? input = null, long? active = null, long? output = null) : this(
        input is null ? null : DateTimeOffset.FromUnixTimeMilliseconds((long)input),
        active is null ? null : DateTimeOffset.FromUnixTimeMilliseconds((long)active),
        output is null ? null : DateTimeOffset.FromUnixTimeMilliseconds((long)output))
    {
    }

    public bool Exist => Input is not null;

    public TimeSpan Total => Exist ?
        Output is null ?
        DateTimeOffset.UtcNow - Time(Input) : Time(Output!) - Time(Input) :
        default;

    static DateTimeOffset Time(DateTimeOffset? value) => ((DateTimeOffset)value!).UtcDateTime!;
}
