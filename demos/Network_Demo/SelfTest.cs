/**
  @file Network_Demo/SelfTest.cs

  Command-line self-check for the Network_Demo message layer.

  Run with --selftest to exercise the serialization the client and server
  exchange over ReliableConduit without needing a network connection or the
  graphics assets.  This is the same BinaryOutput/BinaryInput wire format used
  by the live demo, so it can be checked in environments where the window and
  GPU are unavailable.
 */

using System;
using G3D;

namespace Network_Demo
{
    public static class SelfTest
    {
        private static int failures;

        /** A BinaryOutput's bytes as an array a BinaryInput can read back. */
        private static byte[] commitBuffer(BinaryOutput b)
        {
            byte[] data = new byte[b.size()];
            b.commit(data);
            return data;
        }

        private static BinaryInput readBack(BinaryOutput b)
        {
            byte[] data = commitBuffer(b);
            return new BinaryInput(data, data.Length, G3DEndian.G3D_LITTLE_ENDIAN, false, false);
        }

        private static void Check(bool condition, string name)
        {
            if (!condition)
            {
                failures++;
                Console.WriteLine("FAIL: " + name);
            }
        }

        public static int Run(string[] args)
        {
            // Controls: the desired yaw is written as float32, so the value is
            // quantized exactly as the original writes it.
            Controls controls = new Controls();
            controls.desiredYawVelocity = 1.0 / 3.0;
            controls.desiredVelocity = new Vector3(1.5f, -2.25f, 0.125f);

            BinaryOutput bo = new BinaryOutput();
            controls.serialize(bo);
            BinaryInput bi = readBack(bo);
            Controls controls2 = new Controls();
            controls2.deserialize(bi);

            Check(controls2.desiredVelocity == controls.desiredVelocity, "Controls desiredVelocity round trip");
            Check(Math.Abs(controls2.desiredYawVelocity - (float)controls.desiredYawVelocity) < 1e-6,
                "Controls yaw round trip as float32");

            // SignOnMessage
            SignOnMessage signOn = new SignOnMessage(7);
            BinaryOutput so = new BinaryOutput();
            signOn.serialize(so);
            BinaryInput si = readBack(so);
            SignOnMessage signOn2 = new SignOnMessage();
            signOn2.deserialize(si);
            Check(signOn2.id == 7, "SignOnMessage id round trip");

            // Entity: the fields the server sends when a client joins.
            Entity entity = new Entity();
            entity.id = 42;
            entity.color = Color3.red();
            entity.modelType = Entity.ModelType.HELICOPTER;
            entity.frame.translation = new Vector3(10, 20, 30);
            entity.frame.rotation = new Quat(Matrix3.fromAxisAngle(Vector3.unitY(), 0.5f));
            entity.name = "Maverick";
            entity.velocity = new Vector3(-1, 2, -3);
            entity.controls = controls;

            BinaryOutput eo = new BinaryOutput();
            entity.serialize(eo);
            BinaryInput ei = readBack(eo);
            Entity entity2 = new Entity();
            entity2.deserialize(ei);

            Check(entity2.id == 42, "Entity id round trip");
            Check(entity2.color == entity.color, "Entity color round trip");
            Check(entity2.modelType == Entity.ModelType.HELICOPTER, "Entity modelType round trip");
            Check(entity2.frame.translation == entity.frame.translation, "Entity frame translation round trip");
            Check(entity2.name == "Maverick", "Entity name round trip");
            Check(entity2.velocity == entity.velocity, "Entity velocity round trip");
            Check(entity2.controls.desiredVelocity == entity.controls.desiredVelocity &&
                Math.Abs(entity2.controls.desiredYawVelocity - (float)entity.controls.desiredYawVelocity) < 1e-6,
                "Entity controls round trip");
            Check(entity2.oldFrameTime < -1e30, "Entity deserialize resets oldFrameTime");

            Vector3 checkDir = entity2.frame.rotation.toRotationMatrix().getColumnVector(2);
            Vector3 expectedDir = entity.frame.rotation.toRotationMatrix().getColumnVector(2);
            Check((checkDir - expectedDir).length() < 1e-4f, "Entity frame rotation round trip");

            // EntityStateMessage
            EntityStateMessage msg = new EntityStateMessage();
            entity.makeStateMessage(msg);
            BinaryOutput mo = new BinaryOutput();
            msg.serialize(mo);
            BinaryInput mi = readBack(mo);
            EntityStateMessage msg2 = new EntityStateMessage();
            msg2.deserialize(mi);

            Check(msg2.id == 42, "EntityStateMessage id round trip");
            Check(msg2.frame.translation == msg.frame.translation, "EntityStateMessage frame round trip");
            Check(msg2.velocity == msg.velocity, "EntityStateMessage velocity round trip");
            Check(msg2.controls.desiredVelocity == msg.controls.desiredVelocity &&
                Math.Abs(msg2.controls.desiredYawVelocity - (float)msg.controls.desiredYawVelocity) < 1e-6,
                "EntityStateMessage controls round trip");

            // clientUpdateFromStateMessage must record the delta used to hide the
            // network jump.
            Entity clientView = new Entity();
            clientView.id = 99;
            clientView.frame.translation = new Vector3(5, 5, 5);
            EntityStateMessage sync = new EntityStateMessage();
            sync.id = 99;
            sync.frame.translation = new Vector3(8, 8, 8);
            clientView.clientUpdateFromStateMessage(sync, 1);

            Vector3 delta = clientView.oldDeltaFrame.translation;
            Check((delta - new Vector3(-3, -3, -3)).length() < 1e-4f, "oldDeltaFrame is old minus new");
            CoordinateFrame smoothed = clientView.smoothCoordinateFrame();
            // The value is lerped toward the old frame while the lerp window is open;
            // the exact position is not the point, only that the jump is hidden.
            Check(smoothed.translation.x > 5.0f && smoothed.translation.x < 8.0f,
                "smoothCoordinateFrame interpolates toward the fresh frame");

            // ServerAd round trip includes the base DiscoveryAdvertisement fields.
            ServerAd ad = new ServerAd();
            ad.name = "Enterprise";
            BinaryOutput ao = new BinaryOutput();
            ad.serialize(ao);
            BinaryInput ai = readBack(ao);
            ServerAd ad2 = new ServerAd();
            ad2.deserialize(ai);
            Check(ad2.name == "Enterprise", "ServerAd name round trip");

            // The simulation must advance the frame by the velocity times dt.
            Entity sim = new Entity();
            sim.id = 1;
            sim.frame.translation = Vector3.zero();
            sim.velocity = new Vector3(1, 0, 0);
            sim.controls.desiredVelocity = new Vector3(1, 0, 0);
            sim.doSimulation((SimTime)0.5);
            Check(sim.frame.translation.x > 0, "Entity doSimulation advances translation");

            // simulateEntities drives every entity in the table.
            EntityTable table = new EntityTable();
            table.set(1, sim);
            table.set(2, sim);
            Vector3 before = sim.frame.translation;
            EntitySimulation.simulateEntities(table, (SimTime)0.5);
            Check(sim.frame.translation.x > before.x, "simulateEntities advances every entity");

            // A live client and server on loopback: the full message exchange.
            if (Array.IndexOf(args, "--network") >= 0)
            {
                runLoopbackTest();
            }

            if (failures == 0)
            {
                Console.WriteLine("Network_Demo self-test passed.");
                return 0;
            }

            Console.WriteLine(failures.ToString() + " Network_Demo self-test check(s) failed.");
            return 1;
        }

        /**
          Starts a listen server and a client against it on loopback, and checks
          that the entity exchange completes.  The client and server both run in
          this process, as the original demo's listen server does.

          The demo's Server needs an App for logging and dataDir, so this drives
          the transport directly against the ported NetworkDevice layer, which is
          the same path the demo uses.
         */
        private static void runLoopbackTest()
        {
            NetworkDevice netDevice = new NetworkDevice();
            netDevice.init();
            try
            {
                NetAddress bindAddress = new NetAddress(0x7f000001, (ushort)GamePort.GAME_PORT);
                ReferenceCountedPointer<NetListener> listener = netDevice.createListener((ushort)GamePort.GAME_PORT);
                Check(listener != null && listener.pointer() != null, "listener created");

                ReferenceCountedPointer<ReliableConduit> conduit =
                    netDevice.createReliableConduit(bindAddress);
                Check(conduit != null && conduit.pointer() != null && conduit.pointer().ok(),
                    "reliable conduit connects to the listener");

                // The server accepts, exactly as Server::acceptIncomingClient does
                // (poll first, then block).
                int polls = 0;
                while (!listener.pointer().clientWaiting() && polls < 200)
                {
                    System.Threading.Thread.Sleep(5);
                    ++polls;
                }
                Check(listener.pointer().clientWaiting(), "listener sees the waiting client");

                ReferenceCountedPointer<ReliableConduit> serverConduit = null;
                if (listener.pointer().clientWaiting())
                {
                    serverConduit = listener.pointer().waitForConnection();
                }
                Check(serverConduit != null && serverConduit.pointer() != null,
                    "server accepts the connection");

                // Exchange the demo's messages over the accepted conduit.
                if (serverConduit != null && serverConduit.pointer() != null && conduit.pointer().ok())
                {
                    SignOnMessage signOn = new SignOnMessage(1234);
                    serverConduit.pointer().send((uint)MessageType.SignOnMessage_MSG, signOn);

                    Entity entity = new Entity();
                    entity.id = 1234;
                    entity.name = "Loopback";
                    entity.modelType = Entity.ModelType.HELICOPTER;
                    serverConduit.pointer().send((uint)MessageType.CreateEntityMessage_MSG, entity);

                    // Read them back on the client side, as Client::onNetwork does.
                    int clientPolls = 0;
                    while (clientPolls < 200 &&
                           (!conduit.pointer().messageWaiting() || conduit.pointer().waitingMessageType() != (uint)MessageType.CreateEntityMessage_MSG))
                    {
                        System.Threading.Thread.Sleep(5);
                        ++clientPolls;
                    }

                    bool gotId = false;
                    bool gotEntity = false;
                    while (conduit.pointer().messageWaiting() && conduit.pointer().ok())
                    {
                        switch ((MessageType)conduit.pointer().waitingMessageType())
                        {
                            case MessageType.SignOnMessage_MSG:
                                {
                                    SignOnMessage m = new SignOnMessage();
                                    conduit.pointer().receive(m);
                                    gotId = (m.id == 1234);
                                }
                                break;

                            case MessageType.CreateEntityMessage_MSG:
                                {
                                    Entity e = new Entity();
                                    conduit.pointer().receive(e);
                                    gotEntity = (e.id == 1234) && (e.name == "Loopback");
                                }
                                break;

                            default:
                                conduit.pointer().receive();
                                break;
                        }
                    }

                    Check(gotId, "client received the SignOnMessage");
                    Check(gotEntity, "client received the CreateEntityMessage");
                }
            }
            finally
            {
                netDevice.cleanup();
            }
        }
    }
}
