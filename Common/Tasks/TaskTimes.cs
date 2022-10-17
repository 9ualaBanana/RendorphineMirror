using Newtonsoft.Json;

namespace Common.Tasks;

public record TaskTimes(long? Input = null, long? Active = null, long? Output = null, long? Finished = null, long? Canceled = null, long? Failed = null)
{
    [JsonIgnore] public DateTimeOffset? InputTime => FromLong(Input);
    [JsonIgnore] public DateTimeOffset? ActiveTime => FromLong(Active);
    [JsonIgnore] public DateTimeOffset? OutputTime => FromLong(Output);
    [JsonIgnore] public DateTimeOffset? FinishedTime => FromLong(Finished);
    [JsonIgnore] public DateTimeOffset? CanceledTime => FromLong(Canceled);
    [JsonIgnore] public DateTimeOffset? FailedTime => FromLong(Failed);

    static DateTimeOffset? FromLong(long? time) => time is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(time.Value);


    public bool Exist => Input is not null;

    public TimeSpan Total => Exist ? (OutputTime ?? DateTimeOffset.UtcNow) - InputTime!.Value : default;
}
