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

                try
                {
                    var mappings = await device.GetAllMappingsAsync();
                    map(mappings, "renderphine", Settings.BUPnpPort);
                    map(mappings, "renderphine-srv", Settings.BUPnpServerPort);
                    map(mappings, "renderphine-dht", Settings.BDhtPort);
                    map(mappings, "renderphine-trt", Settings.BTorrentPort);

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


                void map(Mapping[] mappings, string name, IDatabaseValueBindable<ushort> portb)
                {
                    name += "-" + Environment.MachineName;
                    var mapping = mappings.FirstOrDefault(x => x.Description == name);
                    if (mapping is not null)
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
                                if (mappings.Any(x => x.PublicPort == port)) continue;

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