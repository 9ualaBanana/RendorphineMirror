using Mono.Nat;

namespace Node.Services.Targets;

public class PortsForwardedTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required SettingsInstance Settings { get; init; }
    public required ILogger<PortsForwardedTarget> Logger { get; init; }

    async Task IServiceTarget.ExecuteAsync()
    {
        const int mapTimeSec = 60 * 30;

        int consec = 0;
        var devices = new List<INatDevice>();

        var token = new CancellationTokenSource();
        NatUtility.DeviceFound += (_, args) => found(args).Consume();
        NatUtility.StartDiscovery(NatProtocol.Upnp);

        new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep((mapTimeSec - (60 * 10)) * 1000);

                token.Cancel();
                token = new CancellationTokenSource();

                NatUtility.StopDiscovery();
                NatUtility.StartDiscovery(NatProtocol.Upnp);
            }
        })
        { IsBackground = true }.Start();

        async Task found(DeviceEventArgs args)
        {
            var device = args.Device;
            if (devices.Contains(device) || devices.Any(d => d.DeviceEndpoint == device.DeviceEndpoint))
                return;

            devices.Add(device);
            Logger.Info($"Found device ip {device.DeviceEndpoint} ({await device.GetExternalIPAsync()})");

            try { await mapDevice(device); }
            catch (Exception ex)
            {
                Logger.Error($"Exception while mapping device {device.DeviceEndpoint}: {ex}");
                return;
            }
        }
        async Task mapDevice(INatDevice device)
        {
            Logger.Trace($"Remapping {device.DeviceEndpoint} ({device.GetExternalIP()})");

            try
            {
                /*
                    Mono.Nat GetAllMappingsAsync() uses this abomination
                    for (int i = 0; i < 1000; i++)
                        var message = new GetGenericPortMappingEntry (i, this);

                    And then catches error 713 SpecifiedArrayIndexInvalid to know it got to the end
                    But, some routers return 501 ActionFailed instead
                    And, apparently, error that *should* be catched is 714 NoSuchEntryInArray (as per RFC 6970 5.7)
                    So idk whats going on, dont use this method
                */

                await mapPort(device, "renderphine", Settings.BUPnpPort);
                await mapPort(device, "renderphine-dht", Settings.BDhtPort);
                await mapPort(device, "renderphine-trt", Settings.BTorrentPort);

                consec = 0;
            }
            catch (Exception ex)
            {
                consec++;
                if (consec <= 3)
                    Logger.Error($"Could not create mapping: {ex}");

                await Task.Delay(5000);
                await mapDevice(device);
            }


            async Task mapPort(INatDevice device, string name, IDatabaseValueBindable<ushort> portb)
            {
                name += "-" + Environment.MachineName;

                var mapping = null as Mapping;
                try { mapping = await device.GetSpecificMappingAsync(Protocol.Tcp, portb.Value); }
                catch (MappingException) { }

                // var mapping = mappings.FirstOrDefault(x => x.Description == name);
                if (mapping?.Description == name)
                {
                    Logger.Info($"Found already existing mapping {mapping}. Remapping...");

                    device.DeletePortMap(mapping);
                    device.CreatePortMap(new Mapping(Protocol.Tcp, portb.Value, portb.Value, mapTimeSec, name));
                }
                else
                {
                    Logger.Info("Could not find valid existing mapping");

                    try
                    {
                        for (ushort port = portb.Value; port < ushort.MaxValue; port++)
                        {
                            try
                            {
                                await device.GetSpecificMappingAsync(Protocol.Tcp, port);
                                continue;
                            }
                            catch (MappingException) { }

                            Logger.Info($"Creating mapping {name} on port {port}");
                            portb.Value = port;
                            device.CreatePortMap(new Mapping(Protocol.Tcp, port, port, 0, name));

                            break;
                        }
                    }
                    catch (MappingException ex) { Logger.Error(ex); }
                }
            }
        }
    }
}
