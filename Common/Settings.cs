namespace Common
{
    // TODO: make non-static
    public static class Settings
    {
        public static event Action OnUpdate = delegate { };

        // TODO: make \/ bindable
        public static int ListenPort => 5000; // Get<int>("listenport");

        public static string? SessionId { get => BSessionId.Value; set => BSessionId.Value = value; }
        public static string? UserId { get => BUserId.Value; set => BUserId.Value = value; }
        public static string? Username { get => BUsername.Value; set => BUsername.Value = value; }
        public static string? Language { get => BLanguage.Value; set => BLanguage.Value = value; }
        public static LogLevel LogLevel { get => BLogLevel.Value; set => BLogLevel.Value = value; }

        public static readonly Bindable<string?> BSessionId, BUsername, BUserId, BLanguage;
        public static readonly Bindable<LogLevel> BLogLevel;

        static Settings()
        {
            BSessionId = CreateBindable<string?>();
            BUsername = CreateBindable<string?>();
            BUserId = CreateBindable<string?>();
            BLanguage = CreateBindable<string?>();
            BLogLevel = CreateBindable(LogLevel.Basic);


            Bindable<T> CreateBindable<T>(T defaultValue = default!)
            {
                var bindable = new Bindable<T>(defaultValue!);
                bindable.Changed += (_, _) => OnUpdate();
                // TODO: update values in db when changed

                return bindable;
            }
        }


        // TODO: sqliteconnection stuff
    }
}