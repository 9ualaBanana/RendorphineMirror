using System.Globalization;
using System.Reflection;

namespace Node.UI
{
    public readonly struct LocalizedString
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        public static readonly IReadOnlyWeakEventManager ChangeLangWeakEvent = new WeakEventManager();

        static ImmutableDictionary<string, ImmutableDictionary<string, string>> Translations = null!;
        public static string Locale { get; private set; }
        static string? CurrentLocale;


        public readonly string? Key;

        static LocalizedString()
        {
            var settings = App.Instance.Settings;
            settings.BLanguage.Bindable.SubscribeChanged(() =>
            {
                if (Locale != settings.BLanguage.Value)
                    SetLocale(settings.BLanguage.Value!);
            });

            Locale = CultureInfo.CurrentUICulture.Name;
            if (Locale.Length == 0) Locale = "en-US";

            Reload();
        }
        public LocalizedString(string key)
        {
            Key = key;

            if (Debugger.IsAttached && key.Contains('.', StringComparison.Ordinal) && ToString() == key)
                _logger.Error($"Translation '{key}' does not exists in {Locale}");
        }

        public static implicit operator LocalizedString(string value) => new(value);


        public static string[] GetLoadedLocales() => Translations.Keys.ToArray();

        public static void SetLocale(CultureInfo culture) => SetLocale(culture.Name);
        public static void SetLocale(string culture)
        {
            if (CurrentLocale == culture) return;
            CurrentLocale = culture;

            if (!Translations.ContainsKey(culture))
            {
                _logger.Error("Tried to set non-existing translation " + culture);
                return;
            }

            App.Instance.Settings.Language = culture;
            Locale = culture;
            ((WeakEventManager) ChangeLangWeakEvent).Invoke();
        }

        public static void Reload()
        {
            static string? getLocaleName(string res)
            {
                var index = res.LastIndexOf('.');
                if (index == -1) return null;

                var index2 = res.LastIndexOf('.', index - 1);
                if (index2 == -1) return null;

                return res.Substring(index2 + 1, res.Length - index).Replace('_', '-');
            }

            Translations =
                new[] { Assembly.GetCallingAssembly()!, Assembly.GetEntryAssembly()!, Assembly.GetExecutingAssembly()!, }
                .Where(x => x is not null)
                .Distinct()
                .SelectMany(loadlocale)
                .Select(x => ParseFile(x.Item1!, x.Item2))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToImmutableDictionary(x => x.locale, x => x.keys);

            ((WeakEventManager) ChangeLangWeakEvent).Invoke();
            CheckTranslations();


            IEnumerable<(string?, string)> loadlocale(Assembly assembly) =>
                assembly.GetManifestResourceNames()
                    .Where(x => x.EndsWith(".lang"))
                    .Select(x => (getLocaleName(x), assembly.GetManifestResourceStream(x)))
                    .Where(x => x.Item1 is not null && x.Item2 is not null)
                    .Select(x => (x.Item1, new StreamReader(x.Item2!).ReadToEnd() + "\nempty="));
        }
        [Conditional("DEBUG")]
        static void CheckTranslations()
        {
            if (Translations.Count == 0) return;

            var keys = Translations.OrderByDescending(x => x.Value.Count).First().Value.Keys.ToImmutableArray();
            var maxcount = keys.Length;

            foreach (var (key, values) in Translations)
            {
                if (values.Count == maxcount) continue;

                var nonExisting = keys.Except(values.Select(x => x.Key));
                _logger.Error("Translations not found: " + key + " " + string.Join(", ", nonExisting));
            }
        }

        static (string locale, ImmutableDictionary<string, string> keys)? ParseFile(string locale, string content)
        {
            // check if this locale exists
            try
            {
                CultureInfo.GetCultureInfo(locale);
            }
            catch (CultureNotFoundException ex)
            {
                _logger.Error(ex.ToString());
                return null;
            }

            return (locale, content.Split('\n').Select(ParseLine).Where(x => x.HasValue).Select(x => x!.Value).ToImmutableDictionary(x => x.key, x => x.text.Replace("\\n", "\n")));
        }
        static (string key, string text)? ParseLine(string line)
        {
            var commentIndex = line.IndexOf("//");
            if (commentIndex != -1) line = line.Substring(0, commentIndex);

            var index = line.IndexOf('=');
            if (index == -1) return null;

            return (line.Substring(0, index).Trim(), line.Substring(index + 1).Trim());
        }

        public override string ToString() => ToString(Locale);
        public string ToString(CultureInfo culture) => ToString(culture.Name);
        public string ToString(string locale)
        {
            if (Key is null) return string.Empty;

            if (!Translations.TryGetValue(locale, out var trans))
                if (!Translations.TryGetValue("en-US", out trans))
                    return Key;

            if (trans.TryGetValue(Key, out var text)) return text;
            return Key;
        }
        public string With(params object[] values) => string.Format(ToString(), values);

        public static string String(string value) => new LocalizedString(value).ToString();
    }
}
