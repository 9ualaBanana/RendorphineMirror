using Newtonsoft.Json;

namespace Node.Profiling;

internal static class Profiler
{
    static bool HeartbeatLocked = false;
    static Profile? _cachedProfile;


    public static FuncDispose LockHeartbeat()
    {
        HeartbeatLocked = true;
        return new FuncDispose(() => HeartbeatLocked = false);
    }

    internal static async Task<HttpContent> GetAsync()
    {
        if (Benchmark.ShouldBeRun)
            await Benchmark.RunAsync(1 * 1024 * 1024 * 1024).ConfigureAwait(false);

        while (HeartbeatLocked)
            await Task.Delay(100);

        return await BuildProfileAsync();
    }

    static async Task<FormUrlEncodedContent> BuildProfileAsync()
    {
        _cachedProfile ??= await Profile.CreateDefault();
        Benchmark.UpdateValues(_cachedProfile.Hardware);

        var payloadContent = new Dictionary<string, string>()
        {
            ["sessionid"] = Settings.SessionId!,
            ["info"] = JsonConvert.SerializeObject(_cachedProfile, JsonSettings.Lowercase),
        };
        return new FormUrlEncodedContent(payloadContent);
    }
}