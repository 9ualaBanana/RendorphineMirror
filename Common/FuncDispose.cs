namespace Common;

/// <summary> A struct that invokes a function when disposed </summary>
public readonly struct FuncDispose : IDisposable
{
    readonly Action OnDispose;

    public FuncDispose(Action onDispose) => OnDispose = onDispose;

    public void Dispose() => OnDispose();


    public static FuncDispose Create(Action callback) => new(callback);
    public static FuncDispose Create<T>(Func<T> callback) => new(() => callback());
}