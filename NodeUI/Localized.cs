using System.Reflection;
using System.Text;

namespace NodeUI
{
    public static class Localized
    {
        public static class Login
        {
            public static readonly LocalizedString Title, Button, Email, Password, Welcome, RememberMe, ForgotPassword;
            public static readonly LocalizedString AuthCheck, Loading, LoadingDirectories, Waiting;
            public static readonly LocalizedString WrongLoginPassword, EmptyLogin, EmptyPassword, SidExpired;

            static Login() => Init(typeof(Login));
        }
        public static class Menu
        {
            public static readonly LocalizedString Account, AccountQuit, AccountPrices;
            public static readonly LocalizedString Settings;
            public static readonly LocalizedString Help, HelpFaq, HelpSupport, HelpTelegramChat;
            public static readonly LocalizedString Open, Close;

            static Menu() => Init(typeof(Menu));
        }
        public static class Settings
        {
            public static readonly LocalizedString WindowTitle, Autostart, Interval, Language, LogLevel, Button, Minutes;
            public static readonly LocalizedString LogLevelNone, LogLevelBasic, LogLevelAll;

            static Settings() => Init(typeof(Settings));
        }
        public static class General
        {
            public static readonly LocalizedString LoginError, NoInternet, ServerError;
            public static readonly LocalizedString Yes, No, Ok;
            public static readonly LocalizedString InvalidFilePath;
            public static readonly LocalizedString About, Close;
            public static readonly LocalizedString AnotherInstanceIsRunning, Quit, ContinueLoading;

            static General() => Init(typeof(General));
        }
        public static class Tab
        {
            public static readonly LocalizedString Dashboard, Plugins, Benchmark;

            static Tab() => Init(typeof(Tab));
        }
        public static class Size
        {
            public static readonly LocalizedString B, KB, MB, GB, TB;

            static Size() => Init(typeof(Size));
        }

        public static class Lang
        {
            public static readonly LocalizedString Current = new LocalizedString("lang.current");
            public static readonly LocalizedString Russian = new LocalizedString("lang.ru-RU");
            public static readonly LocalizedString English = new LocalizedString("lang.en-US");

            static Lang() => Init(typeof(Lang));

            public static IEnumerable<LocalizedString> GetAll() =>
                Enumerable.Empty<LocalizedString>()
                .Append(Russian)
                .Append(English);
            public static LocalizedString GetByLocale(string locale) =>
                locale.Replace('_', '-') switch
                {
                    "ru-RU" => Russian,
                    "en-US" => English,
                    _ => throw new System.InvalidOperationException()
                };
        }


        /// <summary>
        /// Authomatically initializes inner <see cref="LocalizedString"/>s with their snake case locale equivalents
        /// </summary>
        /// <param name="type">Type of the initializable class</param>
        static void Init(Type type)
        {
            static string convertName(FieldInfo field)
            {
                var sb = new StringBuilder();

                for (int i = 0; i < field.Name.Length; i++)
                {
                    if (i == 0 || i >= field.Name.Length - 1)
                    {
                        sb.Append(char.ToLowerInvariant(field.Name[i]));
                        continue;
                    }

                    if (char.IsUpper(field.Name[i]) && char.IsLower(field.Name[i + 1]))
                        sb.Append("_" + char.ToLowerInvariant(field.Name[i]));
                    else sb.Append(char.ToLowerInvariant(field.Name[i]));
                }

                return sb.ToString();
            }

            var path = type.FullName!.Split('.').Last().Substring("localized.".Length).Replace('+', '.').ToLowerInvariant();

            foreach (var field in type.GetFields().Where(x => x.FieldType == typeof(LocalizedString)))
                if (((LocalizedString) field.GetValue(null)!).Key is null)
                    field.SetValue(null, new LocalizedString(path + "." + convertName(field)));
        }
    }
}