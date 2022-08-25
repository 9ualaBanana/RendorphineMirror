using NLog;

namespace Telegram.Services.Telegram.Updates;

public abstract record TelegramCallbackData<T> where T : struct, Enum
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public T Value;
    public string[] Arguments;

    protected TelegramCallbackData(T value, string[] arguments)
    {
        Value = value;
        Arguments = arguments;
        _logger.Trace("New {CallbackData} is parsed - {Data}; Arguments - {Arguments}", nameof(TelegramCallbackData<T>), Value, string.Join(", ", Arguments));
    }

    public static bool Matches(string callbackData) =>
        Enum.TryParse<T>(callbackData.Split(new char[] { ',', ' ' }, 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).First(), out var _);

    // Parsing to flags combination can be made only knowing concrete enum type that has `FlagAttribute` on it, 'cause it can't be specified as type parameter constraint.
    // All child classes will basically define the same logic in their constructors, i.e. .Aggreagate((r, n) => r |= n) separate enum values from `IEnumerable<T>`.
    protected static IEnumerable<T> ParseEnumValues(string callbackData) =>
        callbackData.Split().First().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<T>);

    public static string[] ParseArguments(string callbackData) =>
        callbackData.Split().Last().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public string Serialize() => Serialize(Value, Arguments);

    public static string Serialize(T callbackData, params object[] args) =>
        $"{callbackData.ToString().Replace(" ", null)} {(args.Any() ? string.Join(',', args) : string.Empty)}";
}
