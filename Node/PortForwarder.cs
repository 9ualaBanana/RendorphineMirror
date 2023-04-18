using Mono.Nat;

namespace Node
{
    public static class PortForwarder
    {
        readonly static Logger Logger = LogManager.GetCurrentClassLogger();

        public static void Initialize() { }
        static PortForwarder()
        {
            int consec = 0;
            NatUtility.DeviceFound += (_, args) => found(args).Consume();
            NatUtility.StartDiscovery(NatProtocol.Upnp);


            async Task found(DeviceEventArgs args)
            {
                var device = args.Device;
                Logger.Info($"[UPnP] Found device ip {device.DeviceEndpoint}");

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

                    await map(device, "renderphine", Settings.BUPnpPort);
                    await map(device, "renderphine-srv", Settings.BUPnpServerPort);
                    await map(device, "renderphine-dht", Settings.BDhtPort);
                    await map(device, "renderphine-trt", Settings.BTorrentPort);

                    consec = 0;
                }
                catch (Exception ex)
                {
                    consec++;
                    if (consec <= 3)
                        Logger.Error($"[UPnP] Could not create mapping: {ex}");

                    await Task.Delay(5000);
                    await found(args);
                }


                async Task map(INatDevice device, string name, IDatabaseValueBindable<ushort> portb)
                {
                    name += "-" + Environment.MachineName;

                    var mapping = null as Mapping;
                    try { mapping = await device.GetSpecificMappingAsync(Protocol.Tcp, portb.Value); }
                    catch (MappingException) { }

                    // var mapping = mappings.FirstOrDefault(x => x.Description == name);
                    if (mapping?.Description == name)
                    {
                        Logger.Info($"[UPnP] Found already existing mapping {mapping}. Remapping...");

                        device.DeletePortMap(mapping);
                        device.CreatePortMap(new Mapping(Protocol.Tcp, portb.Value, portb.Value, 0, name));
                    }
                    else
                    {
                        Logger.Info("[UPnP] Could not find valid existing mapping");

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

                                Logger.Info($"[UPnP] Creating mapping {name} on port {port}");
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
}