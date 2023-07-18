using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;

namespace Common
{
    public readonly record struct HttpOperationResult(HttpResponseMessage Response, int? ErrorCode)
    {
        public HttpStatusCode StatusCode => Response.StatusCode;
        public bool IsSuccessStatusCode => (int) StatusCode >= 200 && (int) StatusCode < 300;
    }

    public readonly struct OperationResult
    {
        public readonly bool Success;
        public readonly string? Message;
        public readonly HttpOperationResult? HttpData { get; init; }

        public OperationResult(bool success, string? message)
        {
            Success = success;
            Message = message;
            HttpData = null;
        }

        [MethodImpl(256)] public string AsString() => Message ?? string.Empty;
        [MethodImpl(256)] public override string? ToString() => $"{{ Success: {Success}{(Success ? null : $", Message: {Message}")} }}";

        public OperationResult<T> As<T>(T? value = default) => new OperationResult<T>(this, value);


        public static OperationResult Err(string? msg = null) => new OperationResult(false, msg);
        public static OperationResult Err<T>(T obj) where T : notnull => Err(obj.ToString());
        public static OperationResult Err(Exception ex) => new OperationResult(false, ex.Message);
        public static OperationResult<T> Err<T>(string? msg = null) => Err(msg);
        public static OperationResult Succ() => new OperationResult(true, null);
        public static OperationResult<T> Succ<T>(T value) => new OperationResult<T>(value);

        public static OperationResult WrapException(Action func, string? info = null) => WrapException(() => { func(); return true; }, info);
        public static ValueTask<OperationResult> WrapException(Func<ValueTask> func, string? info = null) => WrapException(async () => { await func().ConfigureAwait(false); return true; }, info);
        public static OperationResult WrapException(Func<OperationResult> func, string? info = null)
        {
            try { return func(); }
            catch (Exception ex) { return MakeException(ex, info); }
        }
        public static OperationResult<T> WrapException<T>(Func<OperationResult<T>> func, string? info = null)
        {
            try { return func(); }
            catch (Exception ex) { return MakeException(ex, info); }
        }
        public static async ValueTask<OperationResult> WrapException(Func<ValueTask<OperationResult>> func, string? info = null)
        {
            try { return await func(); }
            catch (Exception ex) { return MakeException(ex, info); }
        }
        public static async ValueTask<OperationResult<T>> WrapException<T>(Func<ValueTask<OperationResult<T>>> func, string? info = null)
        {
            try { return await func(); }
            catch (Exception ex) { return MakeException(ex, info); }
        }
        static OperationResult MakeException(Exception ex, string? info)
        {
            if (info is null) return OperationResult.Err(ex);
            return new OperationResult(false, @$"Got an {ex.GetType().Name} {info}: {ex.Message}");
        }

        public static implicit operator bool(OperationResult es) => es.Success;
        public static implicit operator OperationResult(bool success) => new OperationResult(success, null);
    }
    public readonly struct OperationResult<T>
    {
        public bool Success => EString.Success;
        public string? Message => EString.Message;
        public T Result => Value;
        public readonly HttpOperationResult? HttpData => EString.HttpData;

        public readonly OperationResult EString;
        [AllowNull] public readonly T Value;

        public OperationResult(bool success, string? message, T? value = default) : this(new OperationResult(success, message), value) { }
        public OperationResult(T? value) : this(true, value) { }
        public OperationResult(OperationResult es, T? value = default)
        {
            EString = es;
            Value = value;
        }

        public bool TryGet([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out string error)
        {
            value = Value;
            error = Message;
            return Success;
        }

        [MethodImpl(256)] public string AsString() => EString.AsString();
        [MethodImpl(256)] public override string? ToString() => $"{{ Success: {Success}, {(Success ? $"Value: {Value}" : $"Message: {Message}")} }}";

        public OperationResult<T2> As<T2>(Func<T, T2> convert) => new OperationResult<T2>(EString, convert(Value));

        public static implicit operator bool(in OperationResult<T> es) => es.Success;
        public static implicit operator OperationResult<object>(in OperationResult<T> es) => new OperationResult<object>(es.Value);
        public static implicit operator OperationResult<T>(in OperationResult es) => new OperationResult<T>(es);
        public static implicit operator OperationResult<T>(T t) => new OperationResult<T>(true, t);
    }

    public static class OperationResultExtensions
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static OperationResult<T> AsOpResult<T>(this T value) => OperationResult.Succ(value);
        public static ref readonly OperationResult GetResult<T>(in this OperationResult<T> estring) => ref estring.EString;
        public static ValueTask<T> AsVTask<T>(this T value) => ValueTask.FromResult(value);
        public static Task<T> AsTask<T>(this T value) => Task.FromResult(value);
        public static ValueTask<OperationResult<T>> AsTaskResult<T>(this T value) => value.AsOpResult().AsVTask();

        public static void ThrowIfError(this OperationResult opr, string? format = null)
        {
            opr.ThrowIfError(message =>
                format is not null
                ? new Exception(string.Format(format, message))
                : new Exception(message)
            );
        }
        public static T ThrowIfError<T>(this OperationResult<T> opr, string? format = null)
        {
            opr.GetResult().ThrowIfError(format);
            return opr.Value;
        }
        public static async ValueTask ThrowIfError(this ValueTask<OperationResult> opr, string? format = null) => (await opr).ThrowIfError(format);
        public static async ValueTask<T> ThrowIfError<T>(this ValueTask<OperationResult<T>> opr, string? format = null) => (await opr).ThrowIfError(format);

        public static void ThrowIfError(this OperationResult opr, Func<string, Exception> createfunc)
        {
            if (opr) return;
            throw createfunc(opr.AsString());
        }
        public static T ThrowIfError<T>(this OperationResult<T> opr, Func<string, Exception> createfunc)
        {
            opr.GetResult().ThrowIfError(createfunc);
            return opr.Value;
        }
        public static async ValueTask ThrowIfError(this ValueTask<OperationResult> opr, Func<string, Exception> createfunc) => (await opr).ThrowIfError(createfunc);
        public static async ValueTask<T> ThrowIfError<T>(this ValueTask<OperationResult<T>> opr, Func<string, Exception> createfunc) => (await opr).ThrowIfError(createfunc);

        [return: NotNullIfNotNull(nameof(def))]
        public static T? GetValueOrDefault<T>(this OperationResult<T> opr, T? def = default)
        {
            if (!opr) return def;
            return opr.Value;
        }
        [return: NotNullIfNotNull(nameof(def))]
        public static async ValueTask<T?> GetValueOrDefault<T>(this ValueTask<OperationResult<T>> opr, T? def = default) => (await opr).GetValueOrDefault(def);

        public static OperationResult LogIfError(in this OperationResult opr, string? format = null, ILoggable? loggable = null)
        {
            if (!opr)
            {
                var text = opr.AsString();
                if (format is not null)
                    text = string.Format(format, text);

                if (loggable is not null) loggable.LogErr(text);
                else Logger.Error(text);
            }

            return opr;
        }
        public static OperationResult<T> LogIfError<T>(in this OperationResult<T> opr, string? format = null, ILoggable? loggable = null)
        {
            opr.EString.LogIfError(format, loggable);
            return opr;
        }


        #region Next
        // opres => opres
        public static OperationResult Next(in this OperationResult estring, Func<OperationResult> func) => estring ? func() : estring;
        public static OperationResult<TOut> Next<TOut>(in this OperationResult estring, Func<OperationResult<TOut>> func) => estring ? func() : estring;
        public static OperationResult Next<TIn>(in this OperationResult<TIn> estring, Func<TIn, OperationResult> func) => estring ? func(estring.Value) : estring.GetResult();
        public static OperationResult<TOut> Next<TIn, TOut>(in this OperationResult<TIn> estring, Func<TIn, OperationResult<TOut>> func) => estring ? func(estring.Value) : estring.GetResult();

        // opres => Task<opres>
        public static ValueTask<OperationResult> Next(in this OperationResult estring, Func<ValueTask<OperationResult>> func) =>
            estring ? func() : ValueTask.FromResult(estring);
        public static ValueTask<OperationResult<TOut>> Next<TOut>(in this OperationResult estring, Func<ValueTask<OperationResult<TOut>>> func) =>
            estring ? func() : ValueTask.FromResult(estring.As<TOut>());
        public static ValueTask<OperationResult> Next<TIn>(in this OperationResult<TIn> estring, Func<TIn, ValueTask<OperationResult>> func) =>
            estring ? func(estring.Value) : estring.GetResult().AsVTask();
        public static ValueTask<OperationResult<TOut>> Next<TIn, TOut>(in this OperationResult<TIn> estring, Func<TIn, ValueTask<OperationResult<TOut>>> func) =>
            estring ? func(estring.Value) : estring.GetResult().As<TOut>().AsVTask();

        // Task<opres> => opres
        static async ValueTask<TOut> Map<TIn, TOut>(this ValueTask<TIn> task, Func<TIn, TOut> func) => func(await task.ConfigureAwait(false));
        static async ValueTask<TOut> Map<TIn, TOut>(this ValueTask<TIn> task, Func<TIn, ValueTask<TOut>> func) => await func(await task.ConfigureAwait(false)).ConfigureAwait(false);

        public static ValueTask<OperationResult> Next(in this ValueTask<OperationResult> estring, Func<OperationResult> func) => estring.Map(opr => opr.Next(func));
        public static ValueTask<OperationResult<TOut>> Next<TOut>(in this ValueTask<OperationResult> estring, Func<OperationResult<TOut>> func) => estring.Map(opr => opr.Next(func));
        public static ValueTask<OperationResult> Next<TIn>(in this ValueTask<OperationResult<TIn>> estring, Func<TIn, OperationResult> func) => estring.Map(opr => opr.Next(func));
        public static ValueTask<OperationResult<TOut>> Next<TIn, TOut>(in this ValueTask<OperationResult<TIn>> estring, Func<TIn, OperationResult<TOut>> func) => estring.Map(opr => opr.Next(func));

        // Task<opres> => Task<opres>
        public static ValueTask<OperationResult> Next(in this ValueTask<OperationResult> estring, Func<ValueTask<OperationResult>> func) => estring.Map(opr => opr.Next(func));
        public static ValueTask<OperationResult<TOut>> Next<TOut>(in this ValueTask<OperationResult> estring, Func<ValueTask<OperationResult<TOut>>> func) => estring.Map(opr => opr.Next(func));
        public static ValueTask<OperationResult> Next<TIn>(in this ValueTask<OperationResult<TIn>> estring, Func<TIn, ValueTask<OperationResult>> func) => estring.Map(opr => opr.Next(func));
        public static ValueTask<OperationResult<TOut>> Next<TIn, TOut>(in this ValueTask<OperationResult<TIn>> estring, Func<TIn, ValueTask<OperationResult<TOut>>> func) => estring.Map(opr => opr.Next(func));
        #endregion

        #region NextAlways
        // opres => opres
        public static OperationResult NextAlways(in this OperationResult estring, Func<OperationResult> func) => func();
        public static OperationResult<TOut> NextAlways<TOut>(in this OperationResult estring, Func<OperationResult<TOut>> func) => func();
        public static OperationResult NextAlways<TIn>(in this OperationResult<TIn> estring, Func<OperationResult<TIn>, OperationResult> func) => func(estring);
        public static OperationResult<TOut> NextAlways<TIn, TOut>(in this OperationResult<TIn> estring, Func<OperationResult<TIn>, OperationResult<TOut>> func) => func(estring);

        // opres => Task<opres>
        public static ValueTask<OperationResult> NextAlways(in this OperationResult estring, Func<ValueTask<OperationResult>> func) => func();
        public static ValueTask<OperationResult<TOut>> NextAlways<TOut>(in this OperationResult estring, Func<ValueTask<OperationResult<TOut>>> func) => func();
        public static ValueTask<OperationResult> NextAlways<TIn>(in this OperationResult<TIn> estring, Func<OperationResult<TIn>, ValueTask<OperationResult>> func) => func(estring);
        public static ValueTask<OperationResult<TOut>> NextAlways<TIn, TOut>(in this OperationResult<TIn> estring, Func<OperationResult<TIn>, ValueTask<OperationResult<TOut>>> func) => func(estring);

        // Task<opres> => opres
        public static ValueTask<OperationResult> NextAlways(in this ValueTask<OperationResult> estring, Func<OperationResult> func) => estring.Map(_ => func());
        public static ValueTask<OperationResult<TOut>> NextAlways<TOut>(in this ValueTask<OperationResult> estring, Func<OperationResult<TOut>> func) => estring.Map(_ => func());
        public static ValueTask<OperationResult> NextAlways<TIn>(in this ValueTask<OperationResult<TIn>> estring, Func<OperationResult<TIn>, OperationResult> func) => estring.Map(func);
        public static ValueTask<OperationResult<TOut>> NextAlways<TIn, TOut>(in this ValueTask<OperationResult<TIn>> estring, Func<OperationResult<TIn>, OperationResult<TOut>> func) => estring.Map(func);

        // Task<opres> => Task<opres>
        public static ValueTask<OperationResult> NextAlways(in this ValueTask<OperationResult> estring, Func<ValueTask<OperationResult>> func) => estring.Map(_ => func());
        public static ValueTask<OperationResult<TOut>> NextAlways<TOut>(in this ValueTask<OperationResult> estring, Func<ValueTask<OperationResult<TOut>>> func) => estring.Map(_ => func());
        public static ValueTask<OperationResult> NextAlways<TIn>(in this ValueTask<OperationResult<TIn>> estring, Func<OperationResult<TIn>, ValueTask<OperationResult>> func) => estring.Map(func);
        public static ValueTask<OperationResult<TOut>> NextAlways<TIn, TOut>(in this ValueTask<OperationResult<TIn>> estring, Func<OperationResult<TIn>, ValueTask<OperationResult<TOut>>> func) => estring.Map(func);
        #endregion

        #region NextOnError
        // void => opres
        public static OperationResult NextOnError(in this OperationResult estring, Action func)
        {
            if (!estring) func();
            return estring;
        }
        public static OperationResult NextOnError(in this OperationResult estring, Action<OperationResult> func)
        {
            if (!estring) func(estring);
            return estring;
        }
        public static OperationResult<TIn> NextOnError<TIn>(in this OperationResult<TIn> estring, Action func)
        {
            if (!estring) func();
            return estring;
        }
        public static OperationResult<TIn> NextOnError<TIn>(in this OperationResult<TIn> estring, Action<OperationResult<TIn>> func)
        {
            if (!estring) func(estring);
            return estring;
        }

        // Task => opres
        public static ValueTask<OperationResult> NextOnError(in this ValueTask<OperationResult> estring, Action func) => estring.Map(opr => opr.NextOnError(func));
        public static ValueTask<OperationResult> NextOnError(in this ValueTask<OperationResult> estring, Action<OperationResult> func) => estring.Map(opr => opr.NextOnError(func));
        public static ValueTask<OperationResult<TIn>> NextOnError<TIn>(in this ValueTask<OperationResult<TIn>> estring, Action func) => estring.Map(opr => opr.NextOnError(func));
        public static ValueTask<OperationResult<TIn>> NextOnError<TIn>(in this ValueTask<OperationResult<TIn>> estring, Action<OperationResult<TIn>> func) => estring.Map(opr => opr.NextOnError(func));

        // opres => opres
        public static OperationResult NextOnError(in this OperationResult estring, Func<OperationResult> func) => estring ? estring : func();
        public static OperationResult<TOut> NextOnError<TOut>(in this OperationResult estring, Func<OperationResult<TOut>> func) => estring ? estring : func();
        public static OperationResult NextOnError<TIn>(in this OperationResult<TIn> estring, Func<OperationResult<TIn>, OperationResult> func) => estring ? estring.EString : func(estring);
        public static OperationResult<TOut> NextOnError<TIn, TOut>(in this OperationResult<TIn> estring, Func<OperationResult<TIn>, OperationResult<TOut>> func) => estring ? estring.EString : func(estring);

        // opres => Task<opres>
        public static ValueTask<OperationResult> NextOnError(in this OperationResult estring, Func<ValueTask<OperationResult>> func) => estring ? estring.AsVTask() : func();
        public static ValueTask<OperationResult<TOut>> NextOnError<TOut>(in this OperationResult estring, Func<ValueTask<OperationResult<TOut>>> func) => estring ? estring.As<TOut>().AsVTask() : func();
        public static ValueTask<OperationResult> NextOnError<TIn>(in this OperationResult<TIn> estring, Func<OperationResult<TIn>, ValueTask<OperationResult>> func) => estring ? estring.EString.AsVTask() : func(estring);

        // Task<opres> => opres
        public static ValueTask<OperationResult> NextOnError(in this ValueTask<OperationResult> estring, Func<OperationResult> func) => estring.Map(opr => opr.NextOnError(func));
        public static ValueTask<OperationResult<TOut>> NextOnError<TOut>(in this ValueTask<OperationResult> estring, Func<OperationResult<TOut>> func) => estring.Map(opr => opr.NextOnError(func));
        public static ValueTask<OperationResult> NextOnError<TIn>(in this ValueTask<OperationResult<TIn>> estring, Func<OperationResult<TIn>, OperationResult> func) => estring.Map(opr => opr.NextOnError(func));

        // Task<opres> => Task<opres>
        public static ValueTask<OperationResult> NextOnError(in this ValueTask<OperationResult> estring, Func<ValueTask<OperationResult>> func) => estring.Map(opr => opr.NextOnError(func));
        public static ValueTask<OperationResult<TOut>> NextOnError<TOut>(in this ValueTask<OperationResult> estring, Func<ValueTask<OperationResult<TOut>>> func) => estring.Map(opr => opr.NextOnError(func));
        public static ValueTask<OperationResult> NextOnError<TIn>(in this ValueTask<OperationResult<TIn>> estring, Func<OperationResult<TIn>, ValueTask<OperationResult>> func) => estring.Map(opr => opr.NextOnError(func));
        #endregion

        #region Then
        // Task => opres
        static async ValueTask<TOut> MapTask<TOut>(this ValueTask task, Func<TOut> func) { await task.ConfigureAwait(false); return func(); }
        static async ValueTask<TOut> MapTask<TOut>(this ValueTask task, Func<ValueTask<TOut>> func) { await task.ConfigureAwait(false); return await func().ConfigureAwait(false); }
        static async ValueTask<TOut> MapTask<TIn, TOut>(this ValueTask<TIn> task, Func<TIn, TOut> func) => func(await task.ConfigureAwait(false));
        static async ValueTask<TOut> MapTask<TIn, TOut>(this ValueTask<TIn> task, Func<TIn, ValueTask<TOut>> func) => await func(await task.ConfigureAwait(false)).ConfigureAwait(false);
        static async ValueTask ToVTask(this Task task) => await task.ConfigureAwait(false);
        static async ValueTask<T> ToVTask<T>(this Task<T> task) => await task.ConfigureAwait(false);

        public static ValueTask<OperationResult> Then(in this ValueTask task, Func<OperationResult> func) => task.MapTask(func);
        public static ValueTask<OperationResult<TOut>> Then<TOut>(in this ValueTask task, Func<OperationResult<TOut>> func) => task.MapTask(func);
        public static ValueTask<OperationResult> Then<TIn>(in this ValueTask<TIn> task, Func<TIn, OperationResult> func) => task.MapTask(func);
        public static ValueTask<OperationResult<TOut>> Then<TIn, TOut>(in this ValueTask<TIn> task, Func<TIn, OperationResult<TOut>> func) => task.MapTask(func);


        // Task => Task<opres>
        public static ValueTask<OperationResult> Then(in this ValueTask task, Func<ValueTask<OperationResult>> func) => task.MapTask(func);
        public static ValueTask<OperationResult<TOut>> Then<TOut>(in this ValueTask task, Func<ValueTask<OperationResult<TOut>>> func) => task.MapTask(func);
        public static ValueTask<OperationResult> Then<TIn>(in this ValueTask<TIn> task, Func<TIn, ValueTask<OperationResult>> func) => task.MapTask(func);
        public static ValueTask<OperationResult<TOut>> Then<TIn, TOut>(in this ValueTask<TIn> task, Func<TIn, ValueTask<OperationResult<TOut>>> func) => task.MapTask(func);

        public static ValueTask<OperationResult> Then(this Task task, Func<OperationResult> func) => task.ToVTask().Then(func);
        public static ValueTask<OperationResult<TOut>> Then<TOut>(this Task task, Func<OperationResult<TOut>> func) => task.ToVTask().Then(func);
        public static ValueTask<OperationResult> Then<TIn>(this Task<TIn> task, Func<TIn, OperationResult> func) => task.ToVTask().Then(func);
        public static ValueTask<OperationResult<TOut>> Then<TIn, TOut>(this Task<TIn> task, Func<TIn, OperationResult<TOut>> func) => task.ToVTask().Then(func);

        public static ValueTask<OperationResult> Then(this Task task, Func<ValueTask<OperationResult>> func) => task.ToVTask().Then(func);
        public static ValueTask<OperationResult<TOut>> Then<TOut>(this Task task, Func<ValueTask<OperationResult<TOut>>> func) => task.ToVTask().Then(func);
        public static ValueTask<OperationResult> Then<TIn>(this Task<TIn> task, Func<TIn, ValueTask<OperationResult>> func) => task.ToVTask().Then(func);
        public static ValueTask<OperationResult<TOut>> Then<TIn, TOut>(this Task<TIn> task, Func<TIn, ValueTask<OperationResult<TOut>>> func) => task.ToVTask().Then(func);
        #endregion

        #region Merge

        public static OperationResult Merge(this IEnumerable<OperationResult> results)
        {
            foreach (var result in results)
                if (!result)
                    return result;

            return true;
        }

        static OperationResult MergeResults<TIn>(this IEnumerable<OperationResult<TIn>> results, Action<TIn> func)
        {
            foreach (var result in results)
            {
                if (!result) return result.EString;
                func(result.Value);
            }

            return true;
        }
        static OperationResult<List<TOut>> MergeResults<TOut, TIn>(this IEnumerable<OperationResult<TIn>> results, Action<List<TOut>, TIn> func)
        {
            List<TOut> output;
            if (results.TryGetNonEnumeratedCount(out var count)) output = new(count);
            else output = new();

            return results
                .MergeResults(result => func(output, result))
                .Next(() => output.AsOpResult());
        }
        public static OperationResult<List<T>> MergeResults<T>(this IEnumerable<OperationResult<T>> results) =>
            results.MergeResults<T, T>((output, value) => output.Add(value));
        public static OperationResult<List<T>> MergeArrResults<T>(this IEnumerable<OperationResult<List<T>>> results) =>
            results.MergeResults<T, List<T>>((output, value) => output.AddRange(value));
        public static OperationResult<Dictionary<TKey, TVal>> MergeDictResults<TKey, TVal>(this IEnumerable<OperationResult<(TKey key, TVal value)>> results) where TKey : notnull
        {
            var dict = new Dictionary<TKey, TVal>();
            return results
                .MergeResults(value => dict[value.key] = value.value)
                .Next(() => dict.AsOpResult());
        }
        public static OperationResult<Dictionary<TKey, TVal>> MergeDictResults<TKey, TVal>(this IEnumerable<OperationResult<Dictionary<TKey, TVal>>> results) where TKey : notnull
        {
            var dict = new Dictionary<TKey, TVal>();
            return results
                .MergeResults(dic =>
                {
                    foreach (var (key, value) in dic)
                        dict[key] = value;
                })
                .Next(() => dict.AsOpResult());
        }
        public static OperationResult<Dictionary<TKey, TVal>> MergeDictResults<TKey, TVal>(this IEnumerable<OperationResult<KeyValuePair<TKey, TVal>>> results) where TKey : notnull
        {
            var dict = new Dictionary<TKey, TVal>();
            return results
                .MergeResults(dic => dict[dic.Key] = dic.Value)
                .Next(() => dict.AsOpResult());
        }

        public static async ValueTask<OperationResult<List<T>>> MergeResults<T>(this IEnumerable<Task<OperationResult<T>>> results) =>
            (await Task.WhenAll(results)).MergeResults();
        public static async ValueTask<OperationResult<List<T>>> MergeArrResults<T>(this IEnumerable<Task<OperationResult<List<T>>>> results) =>
            (await Task.WhenAll(results)).MergeArrResults();
        public static async ValueTask<OperationResult<Dictionary<TKey, TVal>>> MergeDictResults<TKey, TVal>(this IEnumerable<Task<OperationResult<(TKey key, TVal value)>>> results) where TKey : notnull =>
            (await Task.WhenAll(results)).MergeDictResults();
        public static async ValueTask<OperationResult<Dictionary<TKey, TVal>>> MergeDictResults<TKey, TVal>(this IEnumerable<Task<OperationResult<Dictionary<TKey, TVal>>>> results) where TKey : notnull =>
            (await Task.WhenAll(results)).MergeDictResults();

        public static async ValueTask<OperationResult> MergeParallel(this IEnumerable<ValueTask<OperationResult>> tasks, int limit)
        {
            var result = await MergeParallel(tasks.Select(async x => (await x).As(0)).Select(x => new ValueTask<OperationResult<int>>(x)), limit);
            return result.GetResult();
        }
        public static async ValueTask<OperationResult<T[]>> MergeParallel<T>(this IEnumerable<ValueTask<OperationResult<T>>> tasks, int limit)
        {
            using var throttler = new SemaphoreSlim(Math.Max(1, limit));
            var cancel = false;

            var newtasks = tasks.Select(async task =>
            {
                try
                {
                    await throttler.WaitAsync();
                    if (cancel) return OperationResult.Err<T>();

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

        #endregion
    }
}