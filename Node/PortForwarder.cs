using Mono.Nat;

namespace Node
{
    public static class PortForwarder
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void Initialize() { }
        static PortForwarder()
        {
            NatUtility.DeviceFound += found;
            NatUtility.StartDiscovery(NatProtocol.Upnp);


            static void found(object? _, DeviceEventArgs args)
            {
                var device = args.Device;

                try
                {
                    var mappings = device.GetAllMappings();
                    map(mappings, "renderphine", Settings.BUPnpPort);
                    map(mappings, "renderphine-srv", Settings.BUPnpServerPort);
                    map(mappings, "renderphine-dht", Settings.BDhtPort);
                    map(mappings, "renderphine-trt", Settings.BTorrentPort);
                }
                catch (Exception ex) { _logger.Error(ex, "[UPnP] Could not create mapping: "); }


                void map(Mapping[] mappings, string name, Bindable<ushort> portb)
                {
                    name += "-" + Environment.MachineName;
                    var mapping = mappings.FirstOrDefault(x => x.Description == name);
                    if (mapping is not null)
                    {
                        _logger.Info("[UPnP] Found already existing mapping: {Mapping}", mapping);
                        portb.Value = (ushort) mapping.PublicPort;
                    }
                    else
                    {
                        _logger.Info("[UPnP] Could not find valid existing mapping");

                        try
                        {
                            for (ushort port = portb.Value; port < ushort.MaxValue; port++)
                            {
                                if (mappings.Any(x => x.PublicPort == port)) continue;

                                _logger.Info("[UPnP] Creating mapping {Name} on port {Port}", name, port);
                                portb.Value = port;
                                device.CreatePortMap(new Mapping(Protocol.Tcp, port, port, 0, name));

                                break;
                            }
                        }
                        catch (MappingException ex) { _logger.Error(ex.Message); }
                    }
                }
            }
        }
    }
}