using System.Text;
using Telegram.Callbacks;

namespace Telegram.CallbackQueries;

public class CallbackQuerySerializer
{
    readonly CallbackQuerySerializerOptions _options;

    internal CallbackQuerySerializer(CallbackQuerySerializerOptions? options = default)
    {
        _options = options ?? new CallbackQuerySerializerOptions.Builder().BuildDefault();
    }

    internal string Serialize<ECallbackData>(CallbackQuery<ECallbackData> callbackQuery)
        where ECallbackData : struct, Enum
    {
        var serializedCallbackQuery = new StringBuilder(
            $"{callbackQuery.Data.ToString().Replace(" ", null)}",
            CallbackQuery<ECallbackData>.MaxLength);

        if (callbackQuery.Arguments.Any())
            serializedCallbackQuery
                .Append(_options.DataAndArgumentsSeparator)
                .Append(string.Join(_options.ArgumentsSeparator, callbackQuery.Arguments));

        return serializedCallbackQuery.ToString();
    }

    internal TCallbackQuery? TryDeserialize<TCallbackQuery, ECallbackData>(string callbackQuery)
        where TCallbackQuery : CallbackQuery<ECallbackData>, new()
        where ECallbackData : struct, Enum
    {
        try { return Deserialize<TCallbackQuery, ECallbackData>(callbackQuery); }
        catch { return null; }
    }

    internal TCallbackQuery Deserialize<TCallbackQuery, ECallbackData>(string callbackQuery)
        where TCallbackQuery : CallbackQuery<ECallbackData>, new()
        where ECallbackData : struct, Enum
    {
        var splitCallbackQuery = callbackQuery.Split(_options.DataAndArgumentsSeparator, StringSplitOptions.RemoveEmptyEntries);

        var data = Enum.Parse<ECallbackData>(splitCallbackQuery.First());
        var arguments = splitCallbackQuery.Length > 1 ?
            splitCallbackQuery.Last().Split(_options.ArgumentsSeparator, StringSplitOptions.RemoveEmptyEntries) :
            CallbackQuery<ECallbackData>.EmptyArguments;

        return new CallbackQuery<ECallbackData>.Builder<TCallbackQuery>()
            .Data(data)
            .Arguments(arguments)
            .Build();
    }
}
