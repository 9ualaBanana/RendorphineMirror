using Mono.Nat;

namespace Node
{
    public static class PortForwarder
    {
        public static bool Forwarded { get; private set; }

        // empty method to trigger static ctor
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
                    map(mappings, "renderphine-dht", Settings.BDhtPort);
                    map(mappings, "renderphine-trt", Settings.BTorrentPort);
                }
                catch (Exception ex) { Log.Error("[UPnP] Could not create mapping: " + ex.Message); }


                void map(Mapping[] mappings, string name, Bindable<ushort> portb)
                {
                    name += "-" + Environment.MachineName;
                    var mapping = mappings.FirstOrDefault(x => x.Description == name);
                    if (mapping is not null)
                    {
                        Log.Information($"[UPnP] Found already existing mapping: {mapping}");
                        portb.Value = (ushort) mapping.PublicPort;
                        Forwarded = true;
                    }
                    else
                    {
                        Log.Information($"[UPnP] Could not find valid existing mapping");

                        try
                        {
                            for (ushort port = portb.Value; port < ushort.MaxValue; port++)
                            {
                                if (mappings.Any(x => x.PublicPort == port)) continue;

                                Log.Information($"[UPnP] Creating mapping on port {port} with name {name}");
                                portb.Value = port;
                                device.CreatePortMap(new Mapping(Protocol.Tcp, port, port, 0, name));
                                Forwarded = true;

                                break;
                            }
                        }
                        catch (MappingException ex) { Log.Error(ex.Message); }
                    }
                }
            }
        }
    }
}