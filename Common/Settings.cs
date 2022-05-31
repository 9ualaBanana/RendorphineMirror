using System.Diagnostics.CodeAnalysis;

namespace Common
{
    public static class Settings
    {
        // TODO: make \/ bindable
        public static int ListenPort => 5000; // Get<int>("listenport");

        public static string? SessionId { get => BSessionId.Value; set => BSessionId.Value = value; }
        public static string? UserId { get => BUserId.Value; set => BUserId.Value = value; }
        public static string? Username { get => BUsername.Value; set => BUsername.Value = value; }
        public static string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }
        public static LogLevel LogLevel { get => BLogLevel.Value; set => BLogLevel.Value = value; }

        public static readonly Bindable<string?> BSessionId, BUsername, BUserId, BLanguage;
        public static readonly Bindable<LogLevel> BLogLevel;

        static readonly JsonConfig Config;

        static Settings()
        {
            Config = new JsonConfig(Path.Combine(Init.ConfigDirectory, "config.json"));

            BSessionId = CreateBindable<string?>(nameof(BSessionId), null);
            BUsername = CreateBindable<string?>(nameof(BUsername), null);
            BUserId = CreateBindable<string?>(nameof(BUserId), null);
            BLanguage = CreateBindable<string?>(nameof(BLanguage), null);
            BLogLevel = CreateBindable(nameof(BLogLevel), LogLevel.Basic);


            Bindable<T> CreateBindable<T>(string path, T defaultValue)
            {
                var bindable = new Bindable<T>(Config.TryGet(path, defaultValue!));
                bindable.Changed += (oldv, newv) => Config.Set(path, newv);

                return bindable;
            }
        }
    }
}