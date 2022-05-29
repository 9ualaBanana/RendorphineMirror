using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;

namespace Common
{
    public readonly struct LocalizedString
    {
        public static readonly LocalizedString Empty = new LocalizedString("empty");
        public static readonly IReadOnlyWeakEventManager ChangeLangWeakEvent = new WeakEventManager();

        static ImmutableDictionary<string, ImmutableDictionary<string, string>> Translations = null!;
        public static string Locale { get; private set; }


        public readonly string? Key;

        static LocalizedString()
        {
            Settings.BLanguage.SubscribeChanged((oldv, newv) => SetLocale(newv!));

            Locale = CultureInfo.CurrentUICulture.Name;
            if (Locale.Length == 0) Locale = "en-US";

            Reload();
        }
        public LocalizedString(string key) => Key = key;


        public static string[] GetLoadedLocales() => Translations.Keys.ToArray();

        public static void SetLocale(CultureInfo culture) => SetLocale(culture.Name);
        public static void SetLocale(string culture)
        {
            if (!Translations.ContainsKey(culture))
            {
                Logger.Log("Tried to set non-existing translation");
                return;
            }

            Settings.BLanguage.SetValue(culture);
            Locale = culture;
            ((WeakEventManager) ChangeLangWeakEvent).Invoke();
        }

        public static bool KeyExists(string key) => Translations.ContainsKey(key);
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

            var files = typeof(LocalizedString).Assembly.GetManifestResourceNames().Where(x => x.EndsWith(".lang"));
            var streams = files.Select(x => (getLocaleName(x), typeof(LocalizedString).Assembly.GetManifestResourceStream(x))).Where(x => x.Item1 is { });
            var texts = streams.Where(x => x.Item2 is { }).Select(x => (x.Item1, new StreamReader(x.Item2!).ReadToEnd() + "\nempty="));

            Translations = texts.Select(x => ParseFile(x.Item1!, x.Item2)).Where(x => x.HasValue).Select(x => x!.Value).ToImmutableDictionary(x => x.locale, x => x.keys);
            ((WeakEventManager) ChangeLangWeakEvent).Invoke();
            CheckTranslations();
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
                Logger.LogWarn("Translations not found: " + key + " " + string.Join(", ", nonExisting));
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
                Logger.LogErr(ex);
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
    }
}