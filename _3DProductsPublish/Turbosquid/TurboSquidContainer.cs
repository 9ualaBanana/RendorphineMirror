using System.Net;
using _3DProductsPublish.Turbosquid.Upload;
using Microsoft.Extensions.Logging;

namespace _3DProductsPublish.Turbosquid;

public class TurboSquidContainer
{
    readonly INodeGui _gui;
    readonly ILogger<TurboSquidContainer> _logger;
    readonly Dictionary<string, TurboSquidInstance> _instances = [];

    public TurboSquidContainer(INodeGui gui, ILogger<TurboSquidContainer> logger)
    {
        _gui = gui;
        _logger = logger;
    }

    public TurboSquid? GetCached(string username) => _instances.GetValueOrDefault(username)?.TurboSquid;
    public async Task<TurboSquid> GetAsync(string username, string password, CancellationToken token)
    {
        _logger.LogInformation("Getting the turbo squidder thingamajig");
        if (_instances.TryGetValue(username, out var turbo) && (turbo.Credentials.UserName == username && turbo.Credentials.Password == password))
        {
            _logger.LogInformation("Using cached");
            return turbo.TurboSquid;
        }

        return await ForceGetAsync(username, password, token);
    }
    public async Task<TurboSquid> ForceGetAsync(string username, string password, CancellationToken token)
    {
        _logger.LogInformation("Logging in...");
        var cred = new NetworkCredential(username, password);
        var instance = await TurboSquid.LogInAsyncUsing(cred, _gui, token);
        _instances[username] = new TurboSquidInstance(instance, cred);

        return instance;
    }
    public void ClearCache() => _instances.Clear();


    record TurboSquidInstance(TurboSquid TurboSquid, NetworkCredential Credentials);
}
