namespace _3DProductsPublish.Turbosquid.Network;

// Used to receive responses from CEF browser.
public class ForeignThreadValue<T>
{
    readonly bool _autoReset;

    public ForeignThreadValue(bool autoReset) => _autoReset = autoReset;

    public async Task<T> GetAsync(CancellationToken cancellationToken = default) => await Task.Run(() =>
    {
        _waiter.Wait();
        if (_autoReset) _waiter.Reset();
        return _value!;
    }, cancellationToken);
    public async Task SetAsync(T value, CancellationToken cancellationToken = default) => await Task.Run(() =>
    {
        _value = value;
        _waiter.Set();
    }, cancellationToken);
    T? _value;
    readonly ManualResetEventSlim _waiter = new(false);
}
