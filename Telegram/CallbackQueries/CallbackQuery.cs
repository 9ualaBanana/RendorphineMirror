﻿using Telegram.Bot.Types;

namespace Telegram.CallbackQueries;

/// <summary>
/// <see cref="CallbackQuery.Data"/> wrapper that contains type-specific <typeparamref name="ECallbackData"/>
/// with optional arguments and can be handled by its corresponding <see cref="CallbackQueryHandler{TCallbackQuery, ECallbackData}"/>.
/// </summary>
/// <remarks>
/// <see cref="CallbackQuerySerializer"/> should be used to serialize/deserialize instances of this class to/from <see cref="string"/>.
/// </remarks>
/// <typeparam name="ECallbackData">
/// <see cref="CallbackQuery.Data"/> based on which <see cref="CallbackQuery{ECallbackData}"/> is matched
/// to its corresponding <see cref="CallbackQueryHandler{TCallbackQuery, ECallbackData}"/>.
/// </typeparam>
public abstract record CallbackQuery<ECallbackData>
    where ECallbackData : struct, Enum
{
    internal const int MaxLength = 64;
    internal static object[] EmptyArguments = Array.Empty<object>();

    internal ECallbackData Data { get; private set; }
    internal object[] Arguments { get; private set; } = EmptyArguments;

    internal class Builder<TCallbackQuery>
        where TCallbackQuery : CallbackQuery<ECallbackData>, new()
    {
        readonly TCallbackQuery _callbackQuery = new();

        internal Builder<TCallbackQuery> Data(ECallbackData _)
        { _callbackQuery.Data = _; return this; }

        internal Builder<TCallbackQuery> Arguments(params object[] _)
        { _callbackQuery.Arguments = _; return this; }

        internal TCallbackQuery Build() => _callbackQuery;
    }
}