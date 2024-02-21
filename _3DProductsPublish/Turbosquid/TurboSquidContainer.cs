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

    public async Task<TurboSquid> GetAsync(string username, string password, CancellationToken token)
    {
        _logger.LogInformation("Getting the turbo squidder thingamajig");
        if (_instances.TryGetValue(username, out var turbo) && (turbo.Credentials.UserName == username && turbo.Credentials.Password == password))
            return turbo.TurboSquid;

        _logger.LogInformation("Logging in...");
        var cred = new NetworkCredential(username, password);
        var instance = await TurboSquid.LogInAsyncUsing(cred, _gui, token);
        _instances[username] = new TurboSquidInstance(instance, cred);

        return instance;
    }


    record TurboSquidInstance(TurboSquid TurboSquid, NetworkCredential Credentials);
}
