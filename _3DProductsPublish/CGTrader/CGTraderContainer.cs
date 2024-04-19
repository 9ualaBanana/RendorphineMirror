using Microsoft.Extensions.Logging;
using System.Net;

namespace _3DProductsPublish.CGTrader;

public class CGTraderContainer(INodeGui gui, ILogger<CGTraderContainer> logger)
{
    readonly INodeGui _gui = gui;
    readonly ILogger<CGTraderContainer> _logger = logger;
    readonly Dictionary<string, CGTraderInstance> _instances = [];

    public CGTrader? GetCached(string username) => _instances.GetValueOrDefault(username)?.CGTrader;
    public async Task<CGTrader> GetAsync(string username, string password, CancellationToken token)
    {
        _logger.LogInformation("Getting the turbo squidder thingamajig");
        if (_instances.TryGetValue(username, out var turbo) && (turbo.Credentials.UserName == username && turbo.Credentials.Password == password))
        {
            _logger.LogInformation("Using cached");
            return turbo.CGTrader;
        }

        return await ForceGetAsync(username, password, token);
    }
    public async Task<CGTrader> ForceGetAsync(string username, string password, CancellationToken token)
    {
        _logger.LogInformation("Logging in...");
        var cred = new NetworkCredential(username, password);
        var instance = await CGTrader.LogInAsyncUsing(cred, _gui, token);
        _instances[username] = new CGTraderInstance(instance, cred);

        return instance;
    }
    public void ClearCache() => _instances.Clear();


    record CGTraderInstance(CGTrader CGTrader, NetworkCredential Credentials);
}
