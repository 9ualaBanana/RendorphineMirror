namespace Common;

public interface IOperationResult
{
    bool Success { get; }
    IOperationError Error { get; }
}
public interface IOperationResult<out T> : IOperationResult
{
    IOperationResult OpResult { get; }
    T Value { get; }
}
public interface IErrOperationResult<TSelf> : IOperationResult
    where TSelf : IErrOperationResult<TSelf>
{
    static abstract TSelf Err(OperationResult opres);
}

public readonly struct OperationResult : IOperationResult, IErrOperationResult<OperationResult>
{
    public bool Success { get; }
    public IOperationError Error { get; }

    public OperationResult(bool success, IOperationError? error)
    {
        Success = success;
        Error = error!;
    }

    public override string ToString() => Success ? "Success" : $"Error: {Error}";

    public static bool operator !(OperationResult opres) => !opres.Success;
    public static bool operator true(OperationResult opres) => opres.Success;
    public static bool operator false(OperationResult opres) => !opres.Success;

    public static bool operator &(OperationResult opres, bool b) => opres.Success & b;
    public static bool operator |(OperationResult opres, bool b) => opres.Success | b;
    public static bool operator &(bool b, OperationResult opres) => opres.Success & b;
    public static bool operator |(bool b, OperationResult opres) => opres.Success | b;
    public static implicit operator OperationResult(bool opres) => opres ? Succ() : Err("Unknown error");

    public static OperationResult Succ() => new OperationResult(true, null);
    public static OperationResult Err(IOperationError error) => new OperationResult(false, error);
    public static OperationResult Err(string message) => Err(new StringError(message));
    public static OperationResult Err(Exception ex) => Err(new ExceptionError(ex));

    static OperationResult IErrOperationResult<OperationResult>.Err(OperationResult opres) => new OperationResult(false, opres.Error);

    public static OperationResult<T> Succ<T>(T value) => new(Succ(), value);
    public static OperationResult<T> Err<T>(OperationResult opres) => new(opres, default);
    public static OperationResult<T> Err<T>(IOperationError error) => Err<T>(Err(error));
    public static OperationResult<T> Err<T>(string message) => Err<T>(Err(message));
    public static OperationResult<T> Err<T>(Exception ex) => Err<T>(Err(ex));


    /// <summary> Executes the provided <paramref name="action"/> and returns Success </summary>
    public static OperationResult Execute(Action action)
    {
        action();
        return Succ();
    }

    public static OperationResult WrapException(Action action) =>
        WrapException(() => { action(); return Succ(); });
    public static OperationResult WrapException(Func<OperationResult> action)
    {
        try { return action(); }
        catch (OperationResultException ex) { return Err(ex.Error); }
        catch (Exception ex) { return Err(ex); }
    }

    public static OperationResult<T> WrapException<T>(Func<T> action) =>
        WrapException(() => Succ(action()));
    public static OperationResult<T> WrapException<T>(Func<OperationResult<T>> action)
    {
        try { return action(); }
        catch (Exception ex) { return Err<T>(ex.Message); }
    }

    public static async Task<OperationResult<T>> WrapException<T>(Func<Task<T>> action) =>
        await WrapException(new Func<Task<OperationResult<T>>>(async () => Succ(await action())));
    public static async Task<OperationResult<T>> WrapException<T>(Func<Task<OperationResult<T>>> action)
    {
        try { return await action(); }
        catch (Exception ex) { return Err<T>(ex.Message); }
    }

    public static async ValueTask<OperationResult<T>> WrapException<T>(Func<ValueTask<T>> action) =>
        await WrapException(new Func<ValueTask<OperationResult<T>>>(async () => Succ(await action())));
    public static async ValueTask<OperationResult<T>> WrapException<T>(Func<ValueTask<OperationResult<T>>> action)
    {
        try { return await action(); }
        catch (Exception ex) { return Err<T>(ex.Message); }
    }

    public static async Task<OperationResult> WrapException(Func<Task> action) =>
        await WrapException(new Func<Task<OperationResult>>(async () => { await action(); return Succ(); }));
    public static async Task<OperationResult> WrapException(Func<Task<OperationResult>> action)
    {
        try { return await action(); }
        catch (Exception ex) { return Err(ex.Message); }
    }

    public static async ValueTask<OperationResult> WrapException(Func<ValueTask> action) =>
        await WrapException(new Func<ValueTask<OperationResult>>(async () => { await action(); return Succ(); }));
    public static async ValueTask<OperationResult> WrapException(Func<ValueTask<OperationResult>> action)
    {
        try { return await action(); }
        catch (Exception ex) { return Err(ex.Message); }
    }
}
public readonly struct OperationResult<T> : IOperationResult<T>, IErrOperationResult<OperationResult<T>>
{
    public bool Success => OpResult.Success;
    public IOperationError Error => OpResult.Error;
    IOperationResult IOperationResult<T>.OpResult => OpResult;
    public T Result => Value;

    public OperationResult OpResult { get; }
    public T Value { get; }

    public OperationResult(bool success, IOperationError? error, T? result) : this(new OperationResult(success, error), result) { }
    public OperationResult(OperationResult opResult, T? result)
    {
        OpResult = opResult;
        Value = result!;
    }

    public OperationResult<TNew> Cast<TNew>() => new(OpResult, (TNew) (object) Value!);
    public OperationResult GetResult() => OpResult;

    public override string ToString() => Success ? $"Success: {Value}" : $"Error: {Error}";

    public static bool operator !(OperationResult<T> opres) => !opres.Success;
    public static bool operator true(OperationResult<T> opres) => opres.Success;
    public static bool operator false(OperationResult<T> opres) => !opres.Success;
    public static implicit operator OperationResult<T>(T value) => OperationResult.Succ(value);

    public static bool operator &(OperationResult<T> opres, bool b) => opres.Success & b;
    public static bool operator |(OperationResult<T> opres, bool b) => opres.Success | b;
    public static bool operator &(bool b, OperationResult<T> opres) => opres.Success & b;
    public static bool operator |(bool b, OperationResult<T> opres) => opres.Success | b;
    public static implicit operator OperationResult<T>(OperationResult opres) => new(opres, default);

    static OperationResult<T> IErrOperationResult<OperationResult<T>>.Err(OperationResult opres) => OperationResult.Err<T>(opres);
}