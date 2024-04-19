using System.Net;
using Microsoft.Extensions.Logging;

namespace _3DProductsPublish;

public interface I3DStock<T> where T : I3DStock<T>
{
    static abstract Task<T> LogInAsyncUsing(NetworkCredential credential, INodeGui nodeGui, CancellationToken cancellationToken);
}
public class StockCredentialContainer<T> where T : class, I3DStock<T>
{
    readonly INodeGui _gui;
    readonly ILogger<StockCredentialContainer<T>> _logger;
    readonly Dictionary<string, TurboSquidInstance> _instances = [];

    public StockCredentialContainer(INodeGui gui, ILogger<StockCredentialContainer<T>> logger)
    {
        _gui = gui;
        _logger = logger;
    }

    public T? GetCached(string username) => _instances.GetValueOrDefault(username)?.Value;
    public async Task<T> GetAsync(string username, string password, CancellationToken token)
    {
        _logger.LogInformation("Getting the turbo squidder thingamajig");
        if (_instances.TryGetValue(username, out var turbo) && (turbo.Credentials.UserName == username && turbo.Credentials.Password == password))
        {
            _logger.LogInformation("Using cached");
            return turbo.Value;
        }

        return await ForceGetAsync(username, password, token);
    }
    public async Task<T> ForceGetAsync(string username, string password, CancellationToken token)
    {
        _logger.LogInformation("Logging in...");
        var cred = new NetworkCredential(username, password);
        var instance = await T.LogInAsyncUsing(cred, _gui, token);
        _instances[username] = new TurboSquidInstance(instance, cred);

        return instance;
    }
    public void ClearCache() => _instances.Clear();


    record TurboSquidInstance(T Value, NetworkCredential Credentials);
}
