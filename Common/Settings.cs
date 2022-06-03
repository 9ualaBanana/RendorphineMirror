namespace Common
{
    public static class Settings
    {
        public static string ServerUrl { get => BServerUrl.Value; set => BServerUrl.Value = value; }
        public static ushort ListenPort { get => BListenPort.Value; set => BListenPort.Value = value; }
        public static ushort UPnpPort { get => BUPnpPort.Value; set => BUPnpPort.Value = value; }
        public static string? SessionId { get => BSessionId.Value; set => BSessionId.Value = value; }
        public static string? UserId { get => BUserId.Value; set => BUserId.Value = value; }
        public static string? Username { get => BUsername.Value; set => BUsername.Value = value; }
        public static string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }
        public static LogLevel LogLevel { get => BLogLevel.Value; set => BLogLevel.Value = value; }

        public static readonly Bindable<string> BServerUrl;
        public static readonly Bindable<ushort> BListenPort, BUPnpPort;
        public static readonly Bindable<string?> BSessionId, BUsername, BUserId, BLanguage;
        public static readonly Bindable<LogLevel> BLogLevel;

        static readonly JsonConfig Config;

        static Settings()
        {
            Config = new JsonConfig(Path.Combine(Init.ConfigDirectory, "config.json"));

            BServerUrl = CreateBindable(nameof(BServerUrl), "https://t.microstock.plus:8443");
            BListenPort = CreateBindable<ushort>(nameof(ListenPort), 5123);
            BUPnpPort = CreateBindable<ushort>(nameof(UPnpPort), 5124);
            BSessionId = CreateBindable<string?>(nameof(SessionId), null);
            BUsername = CreateBindable<string?>(nameof(Username), null);
            BUserId = CreateBindable<string?>(nameof(UserId), null);
            BLanguage = CreateBindable<string?>(nameof(Language), null);
            BLogLevel = CreateBindable(nameof(LogLevel), LogLevel.Basic);


            Bindable<T> CreateBindable<T>(string path, T defaultValue)
            {
                var bindable = new Bindable<T>(Config.TryGet(path, defaultValue!));
                bindable.Changed += (oldv, newv) => Config.Set(path, newv);

                return bindable;
            }
        }
    }
}