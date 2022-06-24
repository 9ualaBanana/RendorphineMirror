namespace Common
{
    public readonly struct FuncDispose : IDisposable
    {
        readonly Action OnDispose;

        public FuncDispose(Action onDispose) => OnDispose = onDispose;

        public void Dispose() => OnDispose();
    }
}