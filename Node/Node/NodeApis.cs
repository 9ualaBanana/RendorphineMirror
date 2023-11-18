namespace Node;

public record NodeApis(Api Api, bool LogErrors = true) : Apis(Api, LogErrors)
{
    public required SettingsInstance Settings { get; init; }

    public override string SessionId => Settings.SessionId;

    public NodeApis(Api api) : this(api, true) { }
}
