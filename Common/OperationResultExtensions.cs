using System.Diagnostics.CodeAnalysis;

namespace Common;

[DebuggerStepThrough]
public static class OperationResultExtensions
{
    public static Task<T> AsTask<T>(this T value) => Task.FromResult(value);
    public static ValueTask<T> AsVTask<T>(this T value) => ValueTask.FromResult(value);

    public static OperationResult<T> AsOpResult<T>(this T value) => OperationResult.Succ(value);
    public static OperationResult AsOpResult(this IOperationError error) => OperationResult.Err(error);
    public static OperationResult<T> AsOpResult<T>(this IOperationError error) => OperationResult.Err<T>(error);

    public static T LogIfError<T>(this T opres, ILogger logger, string? format = null, Microsoft.Extensions.Logging.LogLevel level = Microsoft.Extensions.Logging.LogLevel.Error) where T : IOperationResult
    {
        if (!opres.Success)
        {
            var str = opres.Error?.ToString() ?? "Unknown error";
            logger.Log(level, format is null ? str : string.Format(CultureInfo.InvariantCulture, format, str));
        }

        return opres;
    }
    public static async Task<T> LogIfError<T>(this Task<T> opres, ILogger logger, string? format = null, Microsoft.Extensions.Logging.LogLevel level = Microsoft.Extensions.Logging.LogLevel.Error) where T : IOperationResult =>
        (await opres).LogIfError(logger, format, level);

    [return: NotNullIfNotNull(nameof(def))]
    public static T? GetValueOrDefault<T>(this OperationResult<T> opres, T? def = default)
    {
        if (!opres) return def;
        return opres.Value;
    }

    [return: NotNullIfNotNull(nameof(def))]
    public static async Task<T?> GetValueOrDefault<T>(this Task<OperationResult<T>> opres, T? def = default) =>
        (await opres).GetValueOrDefault(def);

    [return: NotNullIfNotNull(nameof(def))]
    public static async ValueTask<T?> GetValueOrDefault<T>(this ValueTask<OperationResult<T>> opres, T? def = default) =>
        (await opres).GetValueOrDefault(def);


    public static void ThrowIfError(this OperationResult opres) =>
        opres.ThrowIfError(err => new OperationResultException(err));
    public static T ThrowIfError<T>(this OperationResult<T> opres)
    {
        opres.OpResult.ThrowIfError();
        return opres.Value;
    }
    public static async Task ThrowIfError(this Task<OperationResult> opres) =>
        (await opres).ThrowIfError();
    public static async Task<T> ThrowIfError<T>(this Task<OperationResult<T>> opres) =>
        (await opres).ThrowIfError();
    public static async ValueTask ThrowIfError(this ValueTask<OperationResult> opres) =>
        (await opres).ThrowIfError();
    public static async ValueTask<T> ThrowIfError<T>(this ValueTask<OperationResult<T>> opres) =>
        (await opres).ThrowIfError();

    public static void ThrowIfError(this OperationResult opres, Func<IOperationError, Exception>? createExceptionFunc = null)
    {
        if (opres.Success) return;
        throw createExceptionFunc?.Invoke(opres.Error) ?? new OperationResultException(opres.Error);
    }
    public static T ThrowIfError<T>(this OperationResult<T> opres, Func<IOperationError, Exception>? createExceptionFunc = null)
    {
        opres.OpResult.ThrowIfError(createExceptionFunc is null ? null : err => createExceptionFunc(err));
        return opres.Value;
    }
    public static async Task ThrowIfError(this Task<OperationResult> opres, Func<IOperationError, Exception>? createExceptionFunc = null) =>
        (await opres).ThrowIfError(createExceptionFunc);
    public static async Task<T> ThrowIfError<T>(this Task<OperationResult<T>> opres, Func<IOperationError, Exception>? createExceptionFunc = null) =>
        (await opres).ThrowIfError(createExceptionFunc);
    public static async ValueTask ThrowIfError(this ValueTask<OperationResult> opres, Func<IOperationError, Exception>? createExceptionFunc = null) =>
        (await opres).ThrowIfError(createExceptionFunc);
    public static async ValueTask<T> ThrowIfError<T>(this ValueTask<OperationResult<T>> opres, Func<IOperationError, Exception>? createExceptionFunc = null) =>
        (await opres).ThrowIfError(createExceptionFunc);

    public static void ThrowIfError(this OperationResult opres, string? format = null) =>
        opres.ThrowIfError(format is null ? null : err => new OperationResultException(err, string.Format(format, err.ToString())));
    public static T ThrowIfError<T>(this OperationResult<T> opres, string? format = null)
    {
        opres.OpResult.ThrowIfError(format);
        return opres.Value;
    }
    public static async Task ThrowIfError(this Task<OperationResult> opres, string? format = null) =>
        (await opres).ThrowIfError(format);
    public static async Task<T> ThrowIfError<T>(this Task<OperationResult<T>> opres, string? format = null) =>
        (await opres).ThrowIfError(format);
    public static async ValueTask ThrowIfError(this ValueTask<OperationResult> opres, string? format = null) =>
        (await opres).ThrowIfError(format);
    public static async ValueTask<T> ThrowIfError<T>(this ValueTask<OperationResult<T>> opres, string? format = null) =>
        (await opres).ThrowIfError(format);


    public static TOut Next<TOut>(this OperationResult opres, Func<TOut> func) where TOut : IErrOperationResult<TOut> =>
        opres.Success ? func() : TOut.Err(opres);
    public static TOut Next<TIn, TOut>(this OperationResult<TIn> opres, Func<TIn, TOut> func) where TOut : IErrOperationResult<TOut> =>
        opres.OpResult.Next(() => func(opres.Value));
    public static async Task<TOut> Next<TOut>(this Task<OperationResult> opres, Func<TOut> func) where TOut : IErrOperationResult<TOut> =>
        (await opres).Next(func);
    public static async Task<TOut> Next<TIn, TOut>(this Task<OperationResult<TIn>> opres, Func<TIn, TOut> func) where TOut : IErrOperationResult<TOut> =>
        (await opres).Next(func);
    public static async Task<TOut> Next<TOut>(this Task<OperationResult> opres, Func<Task<TOut>> func) where TOut : IErrOperationResult<TOut>
    {
        var result = await opres;
        if (result)
            return await func();

        return TOut.Err(result);
    }
    public static async Task<TOut> Next<TIn, TOut>(this Task<OperationResult<TIn>> opres, Func<TIn, Task<TOut>> func) where TOut : IErrOperationResult<TOut>
    {
        var result = await opres;
        if (result)
            return await func(result.Value);

        return TOut.Err(result.OpResult);
    }
    public static async Task<TOut> Next<TOut>(this OperationResult opres, Func<Task<TOut>> func) where TOut : IErrOperationResult<TOut> =>
        opres ? await func() : TOut.Err(opres);
    public static async Task<TOut> Next<TIn, TOut>(this OperationResult<TIn> opres, Func<TIn, Task<TOut>> func) where TOut : IErrOperationResult<TOut> =>
        opres ? await func(opres.Value) : TOut.Err(opres.OpResult);

    public static async ValueTask<TOut> Next<TOut>(this ValueTask<OperationResult> opres, Func<TOut> func) where TOut : IErrOperationResult<TOut> =>
        (await opres).Next(func);
    public static async ValueTask<TOut> Next<TIn, TOut>(this ValueTask<OperationResult<TIn>> opres, Func<TIn, TOut> func) where TOut : IErrOperationResult<TOut> =>
        (await opres).Next(func);
    public static async ValueTask<TOut> Next<TOut>(this ValueTask<OperationResult> opres, Func<ValueTask<TOut>> func) where TOut : IErrOperationResult<TOut>
    {
        var result = await opres;
        if (result)
            return await func();

        return TOut.Err(result);
    }
    public static async ValueTask<TOut> Next<TIn, TOut>(this ValueTask<OperationResult<TIn>> opres, Func<TIn, ValueTask<TOut>> func) where TOut : IErrOperationResult<TOut>
    {
        var result = await opres;
        if (result)
            return await func(result.Value);

        return TOut.Err(result.OpResult);
    }
    public static async ValueTask<TOut> Next<TOut>(this OperationResult opres, Func<ValueTask<TOut>> func) where TOut : IErrOperationResult<TOut> =>
        opres ? await func() : TOut.Err(opres);
    public static async ValueTask<TOut> Next<TIn, TOut>(this OperationResult<TIn> opres, Func<TIn, ValueTask<TOut>> func) where TOut : IErrOperationResult<TOut> =>
        opres ? await func(opres.Value) : TOut.Err(opres.OpResult);
}

[DebuggerStepThrough]
public static class OperationResultCollectionExtensions
{
    public static void AddRange<TKey, TValueTo, TValueFrom>(this IDictionary<TKey, TValueTo> dict, IEnumerable<KeyValuePair<TKey, TValueFrom>> items)
        where TValueFrom : TValueTo
    {
        foreach (var (key, value) in items)
            dict.Add(key, value);
    }

    static IEnumerable<Task<T>> ToAsync<T>(this IEnumerable<T> items) => items.Select(Task.FromResult);

    public static OperationResult<Dictionary<TKey, TValue>> AggregateMany<TKey, TValue>(this IEnumerable<OperationResult<Dictionary<TKey, TValue>>> opresults) where TKey : notnull =>
        opresults.ToAsync().AggregateMany().Result;
    public static OperationResult<List<T>> AggregateMany<T>(this IEnumerable<OperationResult<List<T>>> opresults) =>
        opresults.ToAsync().AggregateMany().Result;
    public static OperationResult<Dictionary<TKey, TValue>> Aggregate<TKey, TValue>(this IEnumerable<OperationResult<KeyValuePair<TKey, TValue>>> opresults) where TKey : notnull =>
        opresults.ToAsync().Aggregate().Result;
    public static OperationResult<List<T>> Aggregate<T>(this IEnumerable<OperationResult<T>> opresults) =>
        opresults.ToAsync().Aggregate().Result;
    public static OperationResult<TResult> Aggregate<TResult, TItem>(this IEnumerable<OperationResult<TItem>> opresults, TResult items) where TResult : ICollection<TItem> =>
        opresults.ToAsync().Aggregate(items).Result;
    public static OperationResult<TResult> Aggregate<TResult, TItem>(this IEnumerable<OperationResult<TItem>> opresults, TResult seed, Action<TResult, TItem> accumulator) =>
        opresults.ToAsync().Aggregate(seed, accumulator).Result;
    public static OperationResult<T> Aggregate<T>(this IEnumerable<OperationResult<T>> opresults, Func<T, T, T> accumulator) =>
        opresults.ToAsync().Aggregate(accumulator).Result;
    public static OperationResult<TResult> Aggregate<TSeed, TResult, TItem>(this IEnumerable<OperationResult<TItem>> opresults, TSeed seed, Func<TSeed, TItem, TResult> accumulator) =>
        opresults.ToAsync().Aggregate(seed, accumulator).Result;

    public static Task<OperationResult<Dictionary<TKey, TValue>>> AggregateMany<TKey, TValue>(this IEnumerable<Task<OperationResult<Dictionary<TKey, TValue>>>> opresults) where TKey : notnull =>
        opresults.Aggregate(new Dictionary<TKey, TValue>(), (seed, items) => seed.AddRange(items));
    public static Task<OperationResult<List<T>>> AggregateMany<T>(this IEnumerable<Task<OperationResult<List<T>>>> opresults) =>
        opresults.Aggregate(new List<T>(), (seed, items) => seed.AddRange(items));
    public static Task<OperationResult<Dictionary<TKey, TValue>>> Aggregate<TKey, TValue>(this IEnumerable<Task<OperationResult<KeyValuePair<TKey, TValue>>>> opresults) where TKey : notnull =>
        opresults.Aggregate(new Dictionary<TKey, TValue>());
    public static Task<OperationResult<List<T>>> Aggregate<T>(this IEnumerable<Task<OperationResult<T>>> opresults) =>
        opresults.Aggregate(new List<T>());
    public static Task<OperationResult<TResult>> Aggregate<TResult, TItem>(this IEnumerable<Task<OperationResult<TItem>>> opresults, TResult items) where TResult : ICollection<TItem> =>
        opresults.Aggregate(items, (items, item) => items.Add(item));
    public static Task<OperationResult<T>> Aggregate<T>(this IEnumerable<Task<OperationResult<T>>> opresults, Func<T, T, T> accumulator)
    {
        var first = true;
        return
            from result in opresults.Aggregate<T, T, T>(default!, (seed, item) =>
            {
                if (first)
                {
                    first = false;
                    return item;
                }

                return accumulator(seed, item);
            })
            select first ? throw new Exception("No items") : result;
    }
    public static Task<OperationResult<TResult>> Aggregate<TResult, TItem>(this IEnumerable<Task<OperationResult<TItem>>> opresults, TResult seed, Action<TResult, TItem> accumulator) =>
        opresults.Aggregate(seed, (result, item) => { accumulator(result, item); return result; });
    public static async Task<OperationResult<TResult>> Aggregate<TSeed, TResult, TItem>(this IEnumerable<Task<OperationResult<TItem>>> opresults, TSeed seed, Func<TSeed, TItem, TResult> accumulator)
    {
        var result = default(TResult)!;
        foreach (var task in opresults)
        {
            var opres = await task;
            if (!opres.Success)
                return OperationResult.Err<TResult>(opres.OpResult.Error);

            result = accumulator(seed, opres.Value);
        }

        return result.AsOpResult();
    }


    public static async Task<OperationResult> AggregateParallel(this IEnumerable<Task<OperationResult>> tasks, int limit) =>
        await
            from result in AggregateParallel(tasks.Select(async x => new OperationResult<int>(await x, default)), limit)
            select OperationResult.Succ();

    public static async Task<OperationResult<T[]>> AggregateParallel<T>(this IEnumerable<Task<OperationResult<T>>> tasks, int limit)
    {
        using var throttler = new SemaphoreSlim(Math.Max(1, limit));
        var cancel = false;

        var newtasks = tasks.Select(async task =>
        {
            try
            {
                await throttler.WaitAsync();
                if (cancel) return OperationResult.Err<T>(new StringError("Cancelled"));

                var result = await task;
                if (!result) cancel = true;

                return result;
            }
            catch (Exception ex) { return OperationResult.Err(ex); }
            finally { throttler.Release(); }
        }).ToArray();


        var results = await Task.WhenAll(newtasks);
        if (cancel) return results.First(x => !x.Success).GetResult();

        return results.Select(x => x.Value).ToArray();
    }
}

[DebuggerStepThrough]
public static class OperationResultLinqExtensions
{
    public static OperationResult Select<T>(this OperationResult<T> opres, Func<T, OperationResult> func) =>
    opres.Next(func);
    public static OperationResult<V> Select<V>(this OperationResult opres, Func<Empty, OperationResult<V>> func) =>
        opres.Next(() => func(default));
    public static OperationResult<V> Select<T, V>(this OperationResult<T> opres, Func<T, OperationResult<V>> func) =>
        opres.Next(func);
    public static OperationResult<V> Select<V>(this OperationResult opres, Func<Empty, V> func) =>
        opres.Next(() => OperationResult.Succ(func(default)));
    public static OperationResult<V> Select<T, V>(this OperationResult<T> opres, Func<T, V> func) =>
        opres.Next(v => OperationResult.Succ(func(v)));

    public static Task<OperationResult> Select<T>(this Task<OperationResult<T>> opres, Func<T, OperationResult> func) =>
        opres.Next(func);
    public static Task<OperationResult<V>> Select<V>(this Task<OperationResult> opres, Func<Empty, OperationResult<V>> func) =>
        opres.Next(() => func(default));
    public static Task<OperationResult<V>> Select<T, V>(this Task<OperationResult<T>> opres, Func<T, OperationResult<V>> func) =>
        opres.Next(func);
    public static Task<OperationResult<V>> Select<V>(this Task<OperationResult> opres, Func<Empty, V> func) =>
        opres.Next(() => OperationResult.Succ(func(default)));
    public static Task<OperationResult<V>> Select<T, V>(this Task<OperationResult<T>> opres, Func<T, V> func) =>
        opres.Next(v => OperationResult.Succ(func(v)));

    public static Task<OperationResult> Select<T>(this Task<OperationResult<T>> opres, Func<T, Task<OperationResult>> func) =>
        opres.Next(func);
    public static Task<OperationResult<V>> Select<V>(this Task<OperationResult> opres, Func<Empty, Task<OperationResult<V>>> func) =>
        opres.Next(() => func(default));
    public static Task<OperationResult<V>> Select<T, V>(this Task<OperationResult<T>> opres, Func<T, Task<OperationResult<V>>> func) =>
        opres.Next(func);
    public static Task<OperationResult<V>> Select<V>(this Task<OperationResult> opres, Func<Empty, Task<V>> func) =>
        opres.Next(async () => OperationResult.Succ(await func(default)));
    public static Task<OperationResult<V>> Select<T, V>(this Task<OperationResult<T>> opres, Func<T, Task<V>> func) =>
        opres.Next(async v => OperationResult.Succ(await func(v)));

    public static Task<OperationResult> Select<T>(this OperationResult<T> opres, Func<T, Task<OperationResult>> func) =>
        opres.Next(func);
    public static Task<OperationResult<V>> Select<V>(this OperationResult opres, Func<Empty, Task<OperationResult<V>>> func) =>
        opres.Next(() => func(default));
    public static Task<OperationResult<V>> Select<T, V>(this OperationResult<T> opres, Func<T, Task<OperationResult<V>>> func) =>
        opres.Next(func);
    public static Task<OperationResult<V>> Select<V>(this OperationResult opres, Func<Empty, Task<V>> func) =>
        opres.Next(new Func<Task<OperationResult<V>>>(async () => OperationResult.Succ(await func(default))));
    public static Task<OperationResult<V>> Select<T, V>(this OperationResult<T> opres, Func<T, Task<V>> func) =>
        opres.Next(new Func<T, Task<OperationResult<V>>>(async v => OperationResult.Succ(await func(v))));


    public static OperationResult<V> SelectMany<T, V>(this OperationResult<T> opres, Func<T, OperationResult> func, Func<T, Empty, V> selector) =>
        opres
            .Next(res1 => func(res1)
            .Next(() => OperationResult.Succ(selector(res1, default))));
    public static OperationResult<V> SelectMany<V>(this OperationResult opres, Func<Empty, OperationResult> func, Func<Empty, Empty, V> selector) =>
        new OperationResult<Empty>(opres, default).SelectMany(func, selector);
    public static OperationResult<V> SelectMany<T, U, V>(this OperationResult<T> opres, Func<T, OperationResult<U>> func, Func<T, U, V> selector) =>
        opres
            .Next(res1 => func(res1)
            .Next(res2 => OperationResult.Succ(selector(res1, res2))));
    public static OperationResult<V> SelectMany<U, V>(this OperationResult opres, Func<Empty, OperationResult<U>> func, Func<Empty, U, V> selector) =>
        new OperationResult<Empty>(opres, default).SelectMany(func, selector);

    public static async Task<OperationResult<V>> SelectMany<T, V>(this Task<OperationResult<T>> opres, Func<T, OperationResult> func, Func<T, Empty, V> selector) =>
        (await opres).SelectMany(func, selector);
    public static async Task<OperationResult<V>> SelectMany<V>(this Task<OperationResult> opres, Func<Empty, OperationResult> func, Func<Empty, Empty, V> selector) =>
        (await opres).SelectMany(func, selector);
    public static async Task<OperationResult<V>> SelectMany<T, U, V>(this Task<OperationResult<T>> opres, Func<T, OperationResult<U>> func, Func<T, U, V> selector) =>
        (await opres).SelectMany(func, selector);
    public static async Task<OperationResult<V>> SelectMany<U, V>(this Task<OperationResult> opres, Func<Empty, OperationResult<U>> func, Func<Empty, U, V> selector) =>
        (await opres).SelectMany(func, selector);

    public static async Task<OperationResult<V>> SelectMany<T, V>(this Task<OperationResult<T>> oprestask, Func<T, Task<OperationResult>> func, Func<T, Empty, V> selector) =>
        await (await oprestask).SelectMany(func, selector);
    public static async Task<OperationResult<V>> SelectMany<V>(this Task<OperationResult> oprestask, Func<Empty, Task<OperationResult>> func, Func<Empty, Empty, V> selector) =>
        await (new OperationResult<Empty>(await oprestask, default)).SelectMany(func, selector);
    public static async Task<OperationResult<V>> SelectMany<T, U, V>(this Task<OperationResult<T>> oprestask, Func<T, Task<OperationResult<U>>> func, Func<T, U, V> selector) =>
        await (await oprestask).SelectMany(func, selector);
    public static async Task<OperationResult<V>> SelectMany<U, V>(this Task<OperationResult> oprestask, Func<Empty, Task<OperationResult<U>>> func, Func<Empty, U, V> selector) =>
        await (new OperationResult<Empty>(await oprestask, default)).SelectMany(func, selector);

    public static async Task<OperationResult<V>> SelectMany<T, V>(this OperationResult<T> opres, Func<T, Task<OperationResult>> func, Func<T, Empty, V> selector) =>
        await opres
            .Next(res1 => func(res1)
            .Next(() => OperationResult.Succ(selector(res1, default))));
    public static async Task<OperationResult<V>> SelectMany<V>(this OperationResult opres, Func<Empty, Task<OperationResult>> func, Func<Empty, Empty, V> selector) =>
        await new OperationResult<Empty>(opres, default).SelectMany(func, selector);
    public static async Task<OperationResult<V>> SelectMany<T, U, V>(this OperationResult<T> opres, Func<T, Task<OperationResult<U>>> func, Func<T, U, V> selector) =>
        await opres
            .Next(res1 => func(res1)
            .Next(res2 => OperationResult.Succ(selector(res1, res2))));
    public static async Task<OperationResult<V>> SelectMany<U, V>(this OperationResult opres, Func<Empty, Task<OperationResult<U>>> func, Func<Empty, U, V> selector) =>
        await new OperationResult<Empty>(opres, default).SelectMany(func, selector);
}