/**
  @file Discovery.h

 <H2>Discovery</H2>
 Discovery is the process by which computers on a Local Area Network (LAN) find
 one another.  The Discovery API allows clients to make a list of servers running
 the same application.  The application typically presents this list to the user
 so he can choose which server to connect to.
 <P>   
 Features of the Discovery API:
 <P>
 Low network traffic
 <BR>  - Broadcasts mainly occur only when a new machine enters/leaves the network
 <BR>Responsive
 <BR>  - Servers appear immediately after launching
 <BR>  - Servers disappear immedatiately after they shut down
 <BR>Extensible
 <BR>  - Add your own game information (e.g. score, num players, map name)
 <BR>Versioned
 <BR>  - Tracks incompatible servers so end-users know to upgrade their client/server

 <H2>Using the Discovery API</H2>

 Subclass DiscoveryAdvertisement to add fields describing a server running your
 application.  For a game, these might be the current map name and the number
 of players.
 <P>
 On the client, create an instance of DiscoveryClient<your advertisement subclass>.
 On the server, create an instance of DiscoveryServer and initialize it with an
 instance of your advertisement subclass.  From your main loop, call doNetwork()
 on the client and server instances.  When your server shuts down, invoke cleanup()
 on it.
   
  @maintainer Morgan McGuire, matrix@graphics3d.com
 
  @created 2003-06-26
  @edited  2005-02-24
 */

using System;

namespace G3D
{
    /**
     If a machine is running two different programs using discovery they
     must have different ports.  However different programs can share the
     same ports if they run on the same LAN with different servers.
     */
    public class DiscoverySettings
    {
        /**
         Name of the program using discovery; used so that mutliple
         programs can use the same discovery ports on the same network.
         */
        public string appProtocolName;

        /**
         Version of the network protocol of the program using discovery.
         Used so that discovery can identify incompatible versions of 
         the server.
         */
        public int appProtocolVersion;

        /**
         Port on which the server broadcasts its identity.    The client
         and server must agree on this value.
         */
        public ushort serverBroadcastPort;

        /**
         Port on which the client broadcasts a server request.  The client
         and server must agree on this value.
         */
        public ushort clientBroadcastPort;

        /**
         Clients connect into this port using a reliable conduit
         to receive the advertisement from a server.  The client
         doesn't look at this value; it uses whatever the server
         sends it.
         */
        public ushort serverAdvertisementPort;

        public NetAddress broadcastAddress;

        /**
         You can use the default G3D discovery ports as long as no other program
         with the same protocol name is using this port.  You <B>can</B>
         run two different G3D discovery programs on the same two ports
         as long as they have different application protocol strings.
         */
        public DiscoverySettings(
            string _appProtocolName,
            int _appProtocolVersion)
            : this(_appProtocolName, _appProtocolVersion, 6173, 6174, 6175)
        {
        }

        public DiscoverySettings(
            string _appProtocolName,
            int _appProtocolVersion,
            ushort _serverBroadcast)
            : this(_appProtocolName, _appProtocolVersion, _serverBroadcast, 6174, 6175)
        {
        }

        public DiscoverySettings(
            string _appProtocolName,
            int _appProtocolVersion,
            ushort _serverBroadcast,
            ushort _clientBroadcast)
            : this(_appProtocolName, _appProtocolVersion, _serverBroadcast, _clientBroadcast, 6175)
        {
        }

        public DiscoverySettings(
            string _appProtocolName,
            int _appProtocolVersion,
            ushort _serverBroadcast,
            ushort _clientBroadcast,
            ushort _serverAdvertisementPort)
        {
            appProtocolName = _appProtocolName;
            appProtocolVersion = _appProtocolVersion;
            serverBroadcastPort = _serverBroadcast;
            clientBroadcastPort = _clientBroadcast;
            serverAdvertisementPort = _serverAdvertisementPort;
            broadcastAddress = NetAddress.broadcastAddress(0);
        }
    }

    /**
     Make your own subclass of this advertisement.  Add fields
     (e.g. numPlayers, currentScore) to increase the amount
     of information advertised.

     Overrides must provide a default constructor.
     */
    public class DiscoveryAdvertisement
    {
        /**
         Address to connect to on the server for the actual game. 
         The IP portion is ignored (the client figures out the IP
         address from the packet itself) but the port is essential.
         Note that this port must not be the discovery port.
         */
        public NetAddress address;

        /**
         (Only used on the client)
         Time since this advertisement was updated.
         */
        public double lastUpdateTime;

        /**
         Overrides must call DiscoveryAdvertisement::serialize(b) first.
         */
        public virtual void serialize(BinaryOutput b)
        {
            address.serialize(b);
        }

        /**
         Overrides must call DiscoveryAdvertisement::deserialize(b) first.
         */
        public virtual void deserialize(BinaryInput b)
        {
            address.deserialize(b);
            lastUpdateTime = System.time();
        }
    }

    /**
     Sent by servers to describe their location.
     */
    public class DiscoveryServerAddressMessage
    {
        /**
         Not part of the message; these settings are used to determine
         if the correct protocol is being used.
         */
        public DiscoverySettings settings;

        /**
         Set to true if this server is running the correct protocol.
         */
        public bool correctProtocol;

        /**
         This is set during the serialize process from the server's settings.
         If different from the client's settings the discovery system will
         classify this server as incompatible.
         */
        public int[] serverProtocolVersion;

        public Array<NetAddress> address;

        public DiscoveryServerAddressMessage()
        {
            settings = null;
            correctProtocol = false;
            serverProtocolVersion = new int[2];
            address = new Array<NetAddress>();
        }

        public DiscoveryServerAddressMessage(DiscoverySettings s)
            : this()
        {
            settings = s;
        }

        public void serialize(BinaryOutput b)
        {
            b.writeString(Discovery.G3D_DISCOVERY_PROTOCOL_NAME);
            b.writeInt32(Discovery.G3D_DISCOVERY_PROTOCOL_VERSION);
            b.writeString(settings.appProtocolName);
            b.writeInt32(settings.appProtocolVersion);

            // Send addresses
            b.writeInt32(address.size());
            for (int i = 0; i < address.size(); ++i)
            {
                address[i].serialize(b);
            }
        }

        public void deserialize(BinaryInput b)
        {
            address.clear();
            correctProtocol = false;
            serverProtocolVersion[0] = 0;
            serverProtocolVersion[1] = 0;

            // Verify that we are on the same protocol
            if (b.readString(Discovery.G3D_DISCOVERY_PROTOCOL_NAME.Length + 1) != Discovery.G3D_DISCOVERY_PROTOCOL_NAME)
            {
                return;
            }

            serverProtocolVersion[0] = b.readInt32();
            if (serverProtocolVersion[0] != Discovery.G3D_DISCOVERY_PROTOCOL_VERSION)
            {
                return;
            }

            if (b.readString() != settings.appProtocolName)
            {
                return;
            }

            serverProtocolVersion[1] = b.readInt32();
            if (serverProtocolVersion[1] != settings.appProtocolVersion)
            {
                return;
            }

            correctProtocol = true;

            address.resize(b.readInt32());
            for (int i = 0; i < address.size(); ++i)
            {
                NetAddress value = address[i];
                value.deserialize(b);
                address[i] = value;
            }
        }
    }

    /**
     Base class for DiscoveryClient and DiscoveryServer.
     */
    public class Discovery
    {
        /**
         Different versions of G3D discovery protocols can't communicate with each other.
         */
        public const string G3D_DISCOVERY_PROTOCOL_NAME = "G3D DISC";
        public const int G3D_DISCOVERY_PROTOCOL_VERSION = 1;

        public NetworkDevice netDevice;
        public DiscoverySettings settings;

        public const int SERVER_SHUTDOWN_MESSAGE = 2;
        public const int SERVER_BROADCAST_MESSAGE = 3;
        public const int CLIENT_BROADCAST_MESSAGE = 4;

        /**
         Only called from subclasses.
         */
        public virtual void init(
            NetworkDevice _netDevice,
            DiscoverySettings _settings)
        {
            Debug.debugAssert(_netDevice != null);
            netDevice = _netDevice;
            settings = _settings;
        }

        protected NetAddress broadcastAddress(ushort port)
        {
            if (settings == null)
            {
                throw new InvalidOperationException("Discovery has not been initialized.");
            }
            return new NetAddress(settings.broadcastAddress.ip(), port);
        }
    }

    public class DiscoveryServer : Discovery, IDisposable
    {
        private sealed class ShutdownMessage
        {
            public void serialize(BinaryOutput b)
            {
                Debug.debugAssert(b != null);
            }

            public void deserialize(BinaryInput b)
            {
                Debug.debugAssert(b != null);
            }
        }

        /**
         For broadcast.
         */
        private ReferenceCountedPointer<LightweightConduit> net;

        /**
         Listen for clients wanting to hear the advertisement over
         a reliable connection.
         */
        private ReferenceCountedPointer<NetListener> listener;

        private DiscoveryAdvertisement advertisement;

        /**
         Broadcast across the lightweight conduit.
         */
        private DiscoveryServerAddressMessage addressMessage;

        /**
         Servers periodically broadcast (unsolicited) in case
         anyone missed the previous message.
         */
        private double lastBroadcast;

        private void sendAnnouncement()
        {
            if (net == null || net.pointer() == null)
            {
                throw new InvalidOperationException("DiscoveryServer has not been initialized.");
            }

            NetAddress broadcast = broadcastAddress(settings.serverBroadcastPort);
            net.pointer().send(broadcast, SERVER_BROADCAST_MESSAGE, addressMessage);
            lastBroadcast = System.time();
        }

        private void sendShutDown()
        {
            if (net == null || net.pointer() == null)
            {
                throw new InvalidOperationException("DiscoveryServer has not been initialized.");
            }

            NetAddress broadcast = broadcastAddress(settings.serverBroadcastPort);
            ShutdownMessage s = new ShutdownMessage();
            net.pointer().send(broadcast, SERVER_SHUTDOWN_MESSAGE, s);
        }

        /**
         You may update the advertisement (synchronously with calling doNetwork)
         after it has been passed in.  This allows a server to change the advertised
         number of players or score for a game, for example.
         */
        public virtual void init(
            NetworkDevice _netDevice,
            DiscoverySettings _settings,
            DiscoveryAdvertisement _advertisement)
        {
            base.init(_netDevice, _settings);

            advertisement = _advertisement;
            addressMessage = new DiscoveryServerAddressMessage(settings);
            netDevice.localHostAddresses(addressMessage.address);

            // Set the port number
            for (int i = 0; i < addressMessage.address.size(); ++i)
            {
                addressMessage.address[i] =
                    new NetAddress(addressMessage.address[i].ip(),
                                   settings.serverAdvertisementPort);
            }

            net = netDevice.createLightweightConduit(settings.clientBroadcastPort, true, true);
            listener = netDevice.createListener(settings.serverAdvertisementPort);

            // Send initial announcement
            sendAnnouncement();
        }

        /**
         Returns true if this discovery server has been initialized
         and is functioning properly.
         */
        public bool ok()
        {
            return listener != null && listener.pointer() != null && listener.pointer().ok() &&
                   net != null && net.pointer() != null && net.pointer().ok();
        }

        /**
         Call periodically to let the server do its job.
         */
        public void doNetwork()
        {
            if (net == null || net.pointer() == null)
            {
                throw new InvalidOperationException("DiscoveryServer has not been initialized.");
            }

            const double UNSOLICITED_BROADCAST_PERIOD = 60;

            // Check for client broadcast requests

            if (net.pointer().messageWaiting())
            {
                // Some client broadcast that it is looking for servers.
                // Respond by sending out our announcement to everyone
                // (avoids having to figure out if the message return address
                // is correct).
                NetAddress dummy = new NetAddress();
                net.pointer().receive(ref dummy);
                sendAnnouncement();
            }
            else if (System.time() > lastBroadcast + UNSOLICITED_BROADCAST_PERIOD)
            {
                sendAnnouncement();
            }

            // Handle incoming connections

            if (listener != null && listener.pointer() != null && listener.pointer().clientWaiting())
            {
                // Respond to this client
                ReferenceCountedPointer<ReliableConduit> client = listener.pointer().waitForConnection();
                if (client != null && client.pointer() != null)
                {
                    try
                    {
                        client.pointer().send(SERVER_BROADCAST_MESSAGE, advertisement);
                    }
                    finally
                    {
                        client.Dispose();
                    }
                }
            }
        }

        /**
         Broadcast a shutdown message.
         */
        public void cleanup()
        {
            sendShutDown();
        }

        public void Dispose()
        {
            if (net != null)
            {
                net.Dispose();
            }
            if (listener != null)
            {
                listener.Dispose();
            }
            advertisement = null;
            GC.SuppressFinalize(this);
        }
    }

    /**
     Used by DiscoveryClient to report servers running a different version
     of this application's protocol.
     */
    public class IncompatibleServerDescription
    {
        public NetAddress address;
        public int[] protocolVersion;
        public double lastUpdateTime;

        public IncompatibleServerDescription()
        {
            protocolVersion = new int[2];
        }

        public string toString()
        {
            return "Incompatible server at " + address.toString() +
                Format.format(", version %d.%d", protocolVersion[0], protocolVersion[1]);
        }
    }

    /**
     Only one DiscoveryClient can be active on a given port at a time on 
     a single computer.

     AdType must be a subclass of DiscoveryAdvertisement.
     */
    public abstract class DiscoveryClientBase : Discovery, IDisposable
    {
        /**
         List of servers running the same application but a different protocol.
         It is useful to show these to users to help them recognize version
         conflicts between client and server.
         Do not access on a second thread while in doNetwork.
         */
        public Array<IncompatibleServerDescription> incompatibleServerList;

        private sealed class BroadcastMessage
        {
            public void serialize(BinaryOutput b)
            {
                Debug.debugAssert(b != null);
            }

            public void deserialize(BinaryInput b)
            {
                Debug.debugAssert(b != null);
            }
        }

        /**
         The client periodically checks servers to make sure they are still up
         and to update its information about them.
         */
        private double lastServerCheck;

        protected ReferenceCountedPointer<LightweightConduit> net;

        protected DiscoveryClientBase()
        {
            incompatibleServerList = new Array<IncompatibleServerDescription>();
            net = new ReferenceCountedPointer<LightweightConduit>();
        }

        /**
         Adds a server to the incompatible list if it is not already there.
         */
        protected void addToIncompatibleList(NetAddress addr, uint p0, uint p1)
        {
            double now = System.time();

            bool alreadyHere = false;

            // Incorrect protocol; add to the incompatible list
            for (int i = 0; i < incompatibleServerList.size(); ++i)
            {
                IncompatibleServerDescription server = incompatibleServerList[i];

                if (server.address == addr)
                {
                    server.lastUpdateTime = now;
                    alreadyHere = true;
                    break;
                }
            }

            if (!alreadyHere)
            {
                IncompatibleServerDescription server = new IncompatibleServerDescription();

                server.lastUpdateTime = now;
                server.address = addr;
                server.protocolVersion[0] = unchecked((int)p0);
                server.protocolVersion[1] = unchecked((int)p1);

                incompatibleServerList.append(server);
            }
        }

        /**
         Tries to connect to the server through the addresses in the array.
         */
        protected void addToServerList(Array<NetAddress> addressArray)
        {
            // Try to connect to each address listed
            for (int a = addressArray.size() - 1; a >= 0; --a)
            {
                NetAddress address = addressArray[a];

                if (readAdvertisement(address))
                {
                    // We've connected to the server
                    break;
                }
                else
                {
                    removeServer(address);
                }
            }
        }

        protected abstract bool readAdvertisement(NetAddress address);

        protected abstract void removeServer(NetAddress address);

        protected abstract void checkRandomServer();

        public override void init(
            NetworkDevice _netDevice,
            DiscoverySettings _settings)
        {
            base.init(_netDevice, _settings);

            lastServerCheck = System.time();

            net.Dispose();
            net = netDevice.createLightweightConduit(settings.serverBroadcastPort, true, true);

            // Send announcement
            NetAddress broadcast = broadcastAddress(settings.clientBroadcastPort);
            BroadcastMessage tmp = new BroadcastMessage();
            net.pointer().send(broadcast, CLIENT_BROADCAST_MESSAGE, tmp);
        }

        /** Shut down the discovery client. */
        public void cleanup()
        {
            if (net != null)
            {
                net.setPointer(null);
            }
        }

        public bool ok()
        {
            return net != null && net.pointer() != null && net.pointer().ok();
        }

        /**
         Call this regularly (several times per second) to
         update the server list.  Not threadsafe-- you must not touch
         the server list while this is running.  This will not block.
         */
        public void doNetwork()
        {
            if (net == null || net.pointer() == null)
            {
                throw new InvalidOperationException("DiscoveryClient has not been initialized.");
            }

            if (net.pointer().messageWaiting())
            {
                NetAddress sender = new NetAddress();

                switch (net.pointer().waitingMessageType())
                {
                    case SERVER_SHUTDOWN_MESSAGE:
                        // Remove the server
                        net.pointer().receive(ref sender);
                        removeServer(sender);
                        break;

                    case SERVER_BROADCAST_MESSAGE:
                        // Check the G3D protocol and the network protocol, then read the ad
                        DiscoveryServerAddressMessage msg = new DiscoveryServerAddressMessage(settings);
                        net.pointer().receive(ref sender, msg);

                        if (msg.correctProtocol && (msg.address.size() > 0))
                        {
                            // Add the actual return address as the first one to be tried.
                            msg.address.append(new NetAddress(sender.ip(), msg.address[0].port()));

                            addToServerList(msg.address);

                        }
                        else
                        {
                            addToIncompatibleList(
                                sender,
                                unchecked((uint)msg.serverProtocolVersion[0]),
                                unchecked((uint)msg.serverProtocolVersion[1]));
                        }
                        break;
                }
            }

            // Periodically re-check servers in the list to see if they crashed
            // (if they shut down, they should have broadcast a shut down message).
            double now = System.time();
            const double UPDATE_TIME_INTERVAL = 30;

            if (now > lastServerCheck + UPDATE_TIME_INTERVAL)
            {
                lastServerCheck = now;
                checkRandomServer();
            }
        }

        public void Dispose()
        {
            cleanup();
            GC.SuppressFinalize(this);
        }
    }

    public class DiscoveryClient<AdType> : DiscoveryClientBase
        where AdType : DiscoveryAdvertisement, new()
    {
        /**
         List of servers.  Do not access on a second thread while in
         doNetwork.
         */
        public Array<AdType> serverList;

        public DiscoveryClient()
        {
            serverList = new Array<AdType>();
        }

        /**
         Returns an index in serverList of the server with the given address.
         Returns -1 if there is none.  Only checks IP addresses.
         */
        private int findServerListIndex(NetAddress addr)
        {
            for (int i = 0; i < serverList.size(); ++i)
            {
                if (addr.ip() == serverList[i].address.ip())
                {
                    return i;
                }
            }

            return -1;
        }

        /**
         Connects to the specified server, reads its advertisement,
         and adds it to the active server list.  Returns true if the server
         can be reached.
         */
        protected override bool readAdvertisement(NetAddress address)
        {
            string hostname = address.toString();

            const double TIMEOUT = 2.0;

            using (ReferenceCountedPointer<ReliableConduit> server = netDevice.createReliableConduit(address))
            {
                if (server == null || server.pointer() == null || !server.pointer().ok())
                {
                    return false;
                }

                AdType advertisement = new AdType();

                // Read the advertisement
                double stopWaiting = System.time() + TIMEOUT;
                bool timedOut = false;

                while (!server.pointer().messageWaiting() && !timedOut && server.pointer().ok())
                {
                    System.sleep(0.1);
                    timedOut = (System.time() > stopWaiting);
                }

                if (timedOut)
                {
                    if (netDevice.log() != null)
                    {
                        netDevice.log().printf("Discovery: Timed out while reading advertisment from %s\n",
                            hostname);
                    }
                    return false;
                }

                if (!server.pointer().ok())
                {
                    if (netDevice.log() != null)
                    {
                        netDevice.log().printf("Discovery: Server %s dropped connection\n", hostname);
                    }
                    return false;
                }

                // Read the advertisement
                Debug.debugAssert(server.pointer().messageWaiting());
                if (!server.pointer().receive(advertisement))
                {
                    if (netDevice.log() != null)
                    {
                        netDevice.log().printf("Discovery: Server %s failed to send advertisement\n", hostname);
                    }
                    return false;
                }

                // Update existing server info or create a new entry
                int index = findServerListIndex(address);
                if (index == -1)
                {
                    index = serverList.size();
                    serverList.resize(index + 1);
                }

                // Update element index
                advertisement.address = address;
                serverList[index] = advertisement;

                return true;
            }
        }

        /**
           Remove this address from our list if we previously
           had a server there.
           */
        protected override void removeServer(NetAddress address)
        {
            int index = findServerListIndex(address);
            if (index > -1)
            {
                serverList.fastRemove(index);
            }
        }

        protected override void checkRandomServer()
        {
            if (serverList.size() >= 1)
            {
                int index = g3dmath.iRandom(0, serverList.size() - 1);

                Array<NetAddress> address = new Array<NetAddress>();
                address.append(serverList[index].address);

                // Remove this server
                serverList.fastRemove(index);

                // Add it back with new info (or leave it removed if no response)
                addToServerList(address);
            }
        }
    }
}
