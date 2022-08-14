using Mono.Nat;

namespace Node
{
    public static class PortForwarder
    {
        readonly static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void Initialize() { }
        static PortForwarder()
        {
            NatUtility.DeviceFound += (_, args) => found(args).Consume();
            NatUtility.StartDiscovery(NatProtocol.Upnp);


            static async Task found(DeviceEventArgs args)
            {
                var device = args.Device;

                try
                {
                    var mappings = await device.GetAllMappingsAsync();
                    map(mappings, "renderphine", Settings.BUPnpPort);
                    map(mappings, "renderphine-srv", Settings.BUPnpServerPort);
                    map(mappings, "renderphine-dht", Settings.BDhtPort);
                    map(mappings, "renderphine-trt", Settings.BTorrentPort);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[UPnP] Could not create mapping: ");

                    await Task.Delay(5000);
                    await found(args);
                }


                void map(Mapping[] mappings, string name, IBindable<ushort> portb)
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