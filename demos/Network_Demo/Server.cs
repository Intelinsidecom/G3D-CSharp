/**
  @file Network_Demo/Server.cs

  Port of Network_Demo/Server.h and Server.cpp.
 */

using System;
using G3D;

namespace Network_Demo
{
    /**
      Server-side representation of a client.
     */
    public class ClientProxy
    {
        /**
          The ID of the object associated with this client.
         */
        public int id;

        /**
          1/2 the ping time.
         */
        public double oneWayLatency;

        public ClientProxy()
        {
            id = 0;
            oneWayLatency = 0;
        }

        public ClientProxy(int _id)
        {
            id = _id;
            oneWayLatency = 0;
        }
    }

    /**
      The server.  This could be used for a dedicated server;
      the program is currently set up to use it only as a listen
      server (client and server on same machine).
     */
    public class Server
    {
        private App app;

        private DiscoveryServer discoveryServer = new DiscoveryServer();

        // Servers send all information over reliable network connections
        // so that the LAN and internet structure of the game is identical.
        // Only matchmaking uses unreliable connections.

        /** Array parallel to clientProxyArray.  Add/remove entries through
            addClient/fastRemoveClient*/
        private Array<ReferenceCountedPointer<ReliableConduit>> clientConduitArray = new Array<ReferenceCountedPointer<ReliableConduit>>();

        private Array<ClientProxy> clientProxyArray = new Array<ClientProxy>();

        private void addClient(ClientProxy p, ReferenceCountedPointer<ReliableConduit> r)
        {
            clientProxyArray.append(p);
            clientConduitArray.append(r);
        }

        private void fastRemoveClient(int i)
        {
            clientProxyArray.fastRemove(i);
            clientConduitArray.fastRemove(i);
        }

        /**
          Listens for new clients.  Handled in doNetwork.
         */
        private ReferenceCountedPointer<NetListener> listener;

        private EntityTable entityTable = new EntityTable();

        /** Generates a fresh ID */
        private int newID()
        {
            ++lastID;
            return lastID;
        }

        // The function-local static in the original; kept at class scope so the
        // sequence is process-global, matching the original's semantics.
        private static int lastID = 0;

        /** Called from doNetwork when listener has an incoming client */
        private void acceptIncomingClient()
        {
            app.debugLog.printf("SERVER --------------------------\n");
            app.debugLog.printf("Incoming client detected\n");

            ClientProxy client = new ClientProxy();
            ReferenceCountedPointer<ReliableConduit> net = null;

            net = listener.pointer().waitForConnection();
            if (net != null && net.pointer() != null)
            {
                client.id = newID();

                // Create a new object for the client
                Entity entity = new Entity();
                entity.id = client.id;
                if (client.id == 1)
                {
                    // Force the first object to black so a single
                    // player helicopter will look good.
                    entity.color = Color3.black();
                }
                else
                {
                    entity.color = Color3.wheelRandom();
                }
                entity.modelType = Entity.ModelType.HELICOPTER;
                entity.frame.translation = Vector3.random() * 10;
                entity.frame.rotation = new Quat(global::G3D.Matrix3.fromAxisAngle(Vector3.unitY(), (float)g3dmath.random(0, g3dmath.twoPi())));
                entity.name = randomName();

                entityTable.set(entity.id, entity);

                // Tell the client their ID
                SignOnMessage msg = new SignOnMessage(entity.id);
                net.pointer().send((uint)MessageType.SignOnMessage_MSG, msg);

                // Tell the client about objects in the world
                // (A drawback of the single-threaded listen server
                // design used in this demo is that we might overflow the
                // network here.  The server should really be running on
                // a separate thread.)

                {
                    Table<int, Entity>.Iterator end = entityTable.end();
                    for (Table<int, Entity>.Iterator e = entityTable.begin(); e != end; e.MoveNext())
                    {
                        net.pointer().send((uint)MessageType.CreateEntityMessage_MSG, e.Current.value);
                    }

                    // Tell the other clients about this new entity
                    ReliableConduit.multisend(clientConduitArray, (uint)MessageType.CreateEntityMessage_MSG, entity);
                }

                // Add this client to our list
                addClient(client, net);

            }
            else
            {
                app.debugLog.printf("Client failed to connect\n");
            }

            app.debugLog.printf("---------------------------------\n");
        }

        public static void doNetworkDiscovery(App app)
        {
        }

        /** The advertisement for this server */
        public ServerAd advertisement = new ServerAd();

        public Server(App app)
        {
            this.app = app;
            app.debugLog.printf("Entering Server::Server\n");
            advertisement.name = app.networkDevice.localHostName();
            discoveryServer.init(app.networkDevice, app.discoverySettings, advertisement);

            app.debugLog.printf("SERVER --------------------------\n");
            listener = app.networkDevice.createListener((ushort)GamePort.GAME_PORT);
            app.debugLog.printf("---------------------------------\n");
            app.debugLog.printf("Leaving Server::Server\n\n");
        }

        public void onNetwork()
        {
            discoveryServer.doNetwork();

            if (listener != null && listener.pointer() != null && listener.pointer().clientWaiting())
            {
                acceptIncomingClient();
            }

            // Check for messages from clients
            for (int c = 0; c < clientProxyArray.size(); ++c)
            {
                ClientProxy clientProxy = clientProxyArray[c];
                ReferenceCountedPointer<ReliableConduit> net = clientConduitArray[c];

                // TODO: check if client is ok, tell others if not

                // TODO: don't stay in this loop for too long
                while (net.pointer().messageWaiting() && net.pointer().ok())
                {
                    switch ((MessageType)net.pointer().waitingMessageType())
                    {
                        case MessageType.NO_MSG:
                            break;

                        case MessageType.EntityStateMessage_MSG:
                            {
                                EntityStateMessage msg = new EntityStateMessage();
                                net.pointer().receive(msg);
                                if (msg.id == clientProxy.id)
                                {
                                    // Entity is a class in this port, so the Table copy
                                    // is a reference; the original mutated the entity
                                    // through a reference into the table.
                                    Entity entity = entityTable.get(msg.id);

                                    // Update the controls of this entity
                                    entity.controls = msg.controls;
                                    entityTable.set(entity.id, entity);
                                    app.debugPrintf("SERVER: receive from %s", entity.name);

                                    // Send to other clients, but don't trust the client's state
                                    // beyond the controls.
                                    entity.makeStateMessage(msg);
                                    ReliableConduit.multisend(clientConduitArray, (uint)MessageType.EntityStateMessage_MSG, msg);
                                }
                                else
                                {
                                    app.debugLog.printf("SERVER: Client sent EntityStateMessage with wrong ID\n\n");
                                }
                            }
                            break;

                        default:
                            app.debugLog.printf("SERVER: Ignored unknown message type %d\n",
                                net.pointer().waitingMessageType());
                            net.pointer().receive();
                            break;
                    }
                }
            }
        }

        private static readonly string[] nameArray = new string[] {
            "Red Baron", "Ace", "Maverick", "Goose", "Iceman", "Angel",
            "Skywalker", "Darth Vader", "Han Solo", "Kirk", "Picard"};

        private static string randomName()
        {
            return nameArray[g3dmath.iRandom(0, 10)];
        }

        public void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            EntitySimulation.simulateEntities(entityTable, sdt);
        }

        public bool ok()
        {
            return discoveryServer.ok() && (listener != null && listener.pointer() != null && listener.pointer().ok());
        }

        public void cleanup()
        {
            listener = null;
            discoveryServer.cleanup();
        }

        public void onGraphics(RenderDevice rd)
        {
            /*
            app->renderDevice->push2D();

                Draw::rect2D(Rect2D::xywh(0,0,200,200), app->renderDevice, Color3::white() * 0.5);
                EntityTable::Iterator end = entityTable.end();
                for (EntityTable::Iterator e = entityTable.begin(); e != end; e.MoveNext()) {
                    Vector3 pos = e->value.frame.translation;
                    Vector3 dir = e->value.frame.toCoordinateFrame().lookVector();

                    pos = Vector3(pos.x + 100, pos.z + 100, 0);
                    dir = Vector3(dir.x, dir.z, 0);

                    Draw::ray(Ray::fromOriginAndDirection(pos, dir), app->renderDevice, e->value.color, 50);
                }
            app->renderDevice->pop2D();
            */
        }
    }
}
