using System;
using System.Threading;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private sealed class DiscoveryTestAdvertisement : DiscoveryAdvertisement
        {
            public string name;
            public int players;

            public DiscoveryTestAdvertisement()
            {
                name = "";
                players = 0;
            }

            public override void serialize(BinaryOutput b)
            {
                base.serialize(b);
                b.writeString(name);
                b.writeInt32(players);
            }

            public override void deserialize(BinaryInput b)
            {
                base.deserialize(b);
                name = b.readString();
                players = b.readInt32();
            }
        }

        private sealed class DiscoveryServerRunner : IDisposable
        {
            private readonly DiscoveryServer server;
            private readonly ManualResetEvent stopEvent;
            private readonly Thread thread;
            private volatile Exception threadException;

            public DiscoveryServerRunner(DiscoveryServer discoveryServer)
            {
                server = discoveryServer;
                stopEvent = new ManualResetEvent(false);
                thread = new Thread(run);
                thread.IsBackground = true;
            }

            public void start()
            {
                thread.Start();
            }

            public Exception exception()
            {
                return threadException;
            }

            private void run()
            {
                try
                {
                    while (!stopEvent.WaitOne(1))
                    {
                        server.doNetwork();
                    }
                }
                catch (Exception e)
                {
                    threadException = e;
                    stopEvent.Set();
                }
            }

            public void Dispose()
            {
                stopEvent.Set();
                thread.Join(5000);
                stopEvent.Dispose();
            }
        }

        private static byte[] serializeDiscoveryMessage(Action<BinaryOutput> serializer)
        {
            BinaryOutput output = new BinaryOutput("<memory>", G3DEndian.G3D_LITTLE_ENDIAN);
            serializer(output);
            byte[] data = new byte[output.getLength()];
            output.commit(data);
            return data;
        }

        private static void testDiscoverySerialization()
        {
            DiscoverySettings defaults = new DiscoverySettings("G3D.Tests.Discovery", 1);
            CheckEqual(6173, defaults.serverBroadcastPort, "Default server broadcast port");
            CheckEqual(6174, defaults.clientBroadcastPort, "Default client broadcast port");
            CheckEqual(6175, defaults.serverAdvertisementPort, "Default advertisement port");
            Check(defaults.broadcastAddress.ip() == 0xFFFFFFFFu, "Default broadcast address");

            DiscoveryTestAdvertisement source = new DiscoveryTestAdvertisement();
            source.address = new NetAddress("127.0.0.1", 1424);
            source.name = "Loopback Server";
            source.players = 3;

            byte[] advertisementData = serializeDiscoveryMessage(delegate(BinaryOutput b)
            {
                source.serialize(b);
            });

            BinaryInput advertisementInput = new BinaryInput(
                advertisementData, advertisementData.Length,
                G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            DiscoveryTestAdvertisement destination = new DiscoveryTestAdvertisement();
            destination.deserialize(advertisementInput);

            Check(destination.address.ip() == source.address.ip(), "Advertisement address IP round trip");
            Check(destination.address.port() == source.address.port(), "Advertisement address port round trip");
            Check(destination.address.Equals(source.address), "Advertisement address equality");
            Check(destination.address == source.address, "Advertisement address round trip");
            CheckEqual("Loopback Server", destination.name, "Advertisement name round trip");
            CheckEqual(3, destination.players, "Advertisement player count round trip");
            Check(destination.lastUpdateTime > 0, "Advertisement update time");

            DiscoverySettings serverSettings = new DiscoverySettings("G3D.Tests.Discovery", 1);
            DiscoveryServerAddressMessage outgoing = new DiscoveryServerAddressMessage(serverSettings);
            outgoing.address.append(new NetAddress("127.0.0.1", 6175));

            byte[] addressData = serializeDiscoveryMessage(delegate(BinaryOutput b)
            {
                outgoing.serialize(b);
            });

            DiscoverySettings matchingSettings = new DiscoverySettings("G3D.Tests.Discovery", 1);
            BinaryInput matchingInput = new BinaryInput(
                addressData, addressData.Length,
                G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            DiscoveryServerAddressMessage matching = new DiscoveryServerAddressMessage(matchingSettings);
            matching.deserialize(matchingInput);

            Check(matching.correctProtocol, "Matching discovery protocol");
            CheckEqual(Discovery.G3D_DISCOVERY_PROTOCOL_VERSION, matching.serverProtocolVersion[0],
                "Discovery protocol version");
            CheckEqual(1, matching.serverProtocolVersion[1], "Application protocol version");
            CheckEqual(1, matching.address.size(), "Discovery address count");
            Check(matching.address[0] == outgoing.address[0], "Discovery address round trip");

            DiscoverySettings incompatibleSettings = new DiscoverySettings("G3D.Tests.Discovery", 2);
            BinaryInput incompatibleInput = new BinaryInput(
                addressData, addressData.Length,
                G3DEndian.G3D_LITTLE_ENDIAN, false, false);
            DiscoveryServerAddressMessage incompatible =
                new DiscoveryServerAddressMessage(incompatibleSettings);
            incompatible.deserialize(incompatibleInput);

            Check(!incompatible.correctProtocol, "Incompatible application protocol");
            CheckEqual(Discovery.G3D_DISCOVERY_PROTOCOL_VERSION,
                incompatible.serverProtocolVersion[0], "Incompatible discovery protocol version");
            CheckEqual(1, incompatible.serverProtocolVersion[1], "Incompatible application version");

            IncompatibleServerDescription description = new IncompatibleServerDescription();
            description.address = new NetAddress("127.0.0.1", 1234);
            description.protocolVersion[0] = 1;
            description.protocolVersion[1] = 2;
            CheckEqual("Incompatible server at 127.0.0.1:1234, version 1.2",
                description.toString(), "Incompatible server description");
        }

        private static void testDiscoveryLoopback(NetworkDevice networkDevice)
        {
            int processId = global::System.Diagnostics.Process.GetCurrentProcess().Id;
            int basePort = 40000 + (processId % 8000) * 3;
            ushort serverBroadcastPort = (ushort)basePort;
            ushort clientBroadcastPort = (ushort)(basePort + 1);
            ushort serverAdvertisementPort = (ushort)(basePort + 2);

            DiscoverySettings settings = new DiscoverySettings(
                "G3D.Tests.Discovery", 1,
                serverBroadcastPort, clientBroadcastPort, serverAdvertisementPort);
            settings.broadcastAddress = new NetAddress("127.0.0.1", 0);

            DiscoveryTestAdvertisement advertisement = new DiscoveryTestAdvertisement();
            advertisement.address = new NetAddress("127.0.0.1", 1424);

            DiscoveryServer server = new DiscoveryServer();
            server.init(networkDevice, settings, advertisement);
            advertisement.name = "Loopback Server";
            advertisement.players = 3;

            DiscoveryClient<DiscoveryTestAdvertisement> client =
                new DiscoveryClient<DiscoveryTestAdvertisement>();
            DiscoveryServerRunner runner = new DiscoveryServerRunner(server);

            try
            {
                Check(server.ok(), "Discovery server initialization");
                runner.start();
                client.init(networkDevice, settings);
                Check(client.ok(), "Discovery client initialization");

                double deadline = System.time() + 5.0;
                while (client.serverList.size() == 0 && System.time() < deadline)
                {
                    client.doNetwork();
                    if (client.serverList.size() == 0)
                    {
                        System.sleep(0.01);
                    }
                }

                CheckEqual(1, client.serverList.size(), "Discovered server count");
                if (client.serverList.size() > 0)
                {
                    CheckEqual("Loopback Server", client.serverList[0].name,
                        "Discovered server name");
                    CheckEqual(3, client.serverList[0].players,
                        "Discovered server player count");
                    Check(client.serverList[0].lastUpdateTime > 0,
                        "Discovered server update time");
                    CheckEqual(serverAdvertisementPort, client.serverList[0].address.port(),
                        "Discovered server address port");
                    Check(client.serverList[0].address.ip() == 0x7F000001u,
                        "Discovered server address IP");
                }

                Check(runner.exception() == null, "Discovery server thread");
                if (runner.exception() != null)
                {
                    Console.Error.WriteLine(runner.exception().ToString());
                }

                server.cleanup();

                deadline = System.time() + 2.0;
                while (client.serverList.size() > 0 && System.time() < deadline)
                {
                    client.doNetwork();
                    if (client.serverList.size() > 0)
                    {
                        System.sleep(0.01);
                    }
                }

                CheckEqual(0, client.serverList.size(), "Discovery server removal");
            }
            finally
            {
                client.cleanup();
                runner.Dispose();
                server.Dispose();
            }
        }

        private static void testDiscovery(NetworkDevice networkDevice)
        {
            printf("Discovery ");
            testDiscoverySerialization();
            testDiscoveryLoopback(networkDevice);
            printf("passed\n");
        }
    }
}
