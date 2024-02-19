using System.Net;
using _3DProductsPublish.Turbosquid.Upload;
using Microsoft.Extensions.Logging;

namespace _3DProductsPublish.Turbosquid;

public class TurboSquidContainer
{
    readonly INodeSettings _settings;
    readonly INodeGui _gui;
    public required ILogger<TurboSquidContainer> Logger { get; init; }
    NetworkCredential? _prevNetworkCredentials;
    TurboSquid? _turboSquid;

    public TurboSquidContainer(INodeSettings settings, INodeGui gui)
    {
        _settings = settings;
        _gui = gui;
    }

    public async Task<TurboSquid> GetAsync(CancellationToken token)
    {
        Logger.LogInformation("Getting the turbo squidder thingamajig");
        if (_turboSquid is not null && (_prevNetworkCredentials is null || (_prevNetworkCredentials.UserName == _settings.TurboSquidUsername && _prevNetworkCredentials.Password == _settings.TurboSquidPassword)))
        {
            Logger.LogInformation("Already logged in and the creds were not changed, returning");
            return _turboSquid;
        }

        Logger.LogInformation("Logging in...");
        return _turboSquid = await TurboSquid.LogInAsyncUsing(_prevNetworkCredentials = new NetworkCredential(_settings.TurboSquidUsername, _settings.TurboSquidPassword), _gui, token);
    }
}
