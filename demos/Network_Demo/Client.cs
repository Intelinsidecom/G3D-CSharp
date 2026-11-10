/**
  @file Network_Demo/Client.cs

  Port of Network_Demo/Client.h and Client.cpp.
 */

using System;
using G3D;

namespace Network_Demo
{
    /** Client's representation of the server */
    public class ServerProxy
    {
        private ReferenceCountedPointer<ReliableConduit> net;
        private App app;
        private Client client;

        public ServerProxy()
        {
            app = null;
            client = null;
        }

        public ServerProxy(App app, Client client)
        {
            this.app = app;
            this.client = client;
        }

        public bool ok()
        {
            return net != null && net.pointer() != null && net.pointer().ok();
        }

        public void connect(NetAddress address)
        {
            app.debugLog.printf("\nCLIENT: --------------------------\n");
            app.debugLog.printf("\nConnecting to \"%s\"\n\n", address.ToString());

            net = app.networkDevice.createReliableConduit(address);

            if (net == null || net.pointer() == null || !net.pointer().ok())
            {
                app.debugLog.printf("\nConnect failed\n\n");
            }
            else
            {
                app.debugLog.printf("\nConnected to server\n\n");
            }

            app.debugLog.printf("--------------------------------\n");
        }

        internal ReferenceCountedPointer<ReliableConduit> getNet()
        {
            return net;
        }
    }

    /**
      This simple demo applet uses the debug mode as the regular
      rendering mode so you can fly around the scene.
     */
    public class Client : GApplet
    {
        private App app;

        private ServerProxy serverProxy;

        private EntityTable entityTable = new EntityTable();

        /** ID of my entity */
        private int localID;

        private GCamera camera = new GCamera();

        /** Called from renderEntities */
        private void renderEntity(Entity entity)
        {
            ReferenceCountedPointer<Helicopter> model = Client.cachedModel;

            if (model == null || model.pointer() == null)
            {
                model = Helicopter.create(app.dataDir);
                Client.cachedModel = model;
            }

            // Lerp between old and new values
            CoordinateFrame cframe = entity.smoothCoordinateFrame();

            // Pose it
            GMaterial material = new GMaterial();
            material.diffuseColor = entity.color;
            material.specularColor = Color3.white();
            model.pointer().render(app.renderDevice, cframe, entity.pose, material);
        }

        /** A function-local static in the original: the model is deliberately
            reused across sessions because App::main can create and destroy a
            Client repeatedly. */
        private static ReferenceCountedPointer<Helicopter> cachedModel;

        /** Called from doGraphics */
        private void renderEntities()
        {
            Table<int, Entity>.Iterator end = entityTable.end();
            for (Table<int, Entity>.Iterator e = entityTable.begin(); e != end; e.MoveNext())
            {
                renderEntity(e.Current.value);
            }
        }

        /** Called from doSimulation */
        private void simulateCamera(SimTime dt)
        {
            Entity me;
            if (entityTable.get(localID, out me))
            {
                const double FOLLOW_DISTANCE = 20;
                const double FOLLOW_HEIGHT = 4;

                CoordinateFrame cframe = me.smoothCoordinateFrame();

                // Move the camera to follow the plane

                camera.setPosition(cframe.translation
                        + cframe.rotation.getColumnVector(2) * (float)FOLLOW_DISTANCE
                        + Vector3.UNIT_Y * (float)FOLLOW_HEIGHT);

                camera.lookAt(cframe.translation + Vector3.UNIT_Y * (float)(FOLLOW_HEIGHT / 2));
            }
        }

        /** GCamera::project has no port; the equivalent projection through the
            current model-view-projection matrix and viewport is computed here. */
        private Vector3 projectToScreen(Vector3 worldPoint)
        {
            Matrix4 mvp = app.renderDevice.getModelViewProjectionMatrix();
            Vector4 v = mvp * new Vector4(worldPoint, 1.0f);

            Rect2D viewport = app.renderDevice.getViewport();

            if (v.w != 0)
            {
                v.x /= v.w;
                v.y /= v.w;
                v.z /= v.w;
            }

            return new Vector3(
                (float)(viewport.x0() + (1 + v.x) * (viewport.x1() - viewport.x0()) / 2),
                (float)(viewport.y0() + (1 + v.y) * (viewport.y1() - viewport.y0()) / 2),
                v.z);
        }

        /** Server to which the client should connect. */
        public ServerAd selectedServer = new ServerAd();

        public Client(App app)
            : base(app)
        {
            this.app = app;
            serverProxy = new ServerProxy(app, this);
        }

        public override void onInit()
        {
            // Called before Client::run() beings
            app.debugCamera.setPosition(new Vector3(0, 2, 10));
            app.debugCamera.lookAt(new Vector3(0, 2, 0));

            localID = Entity.NO_ID;

            // Connect
            serverProxy.connect(selectedServer.address);

            camera.setPosition(Vector3.zero());
            camera.lookAt(-Vector3.UNIT_Z);
            Log.common().printf("Client::init\n");

            setDesiredFrameRate(40);
        }

        public override void onCleanup()
        {
            // Called when Client::run() exits
            entityTable.clear();
        }

        public override void onNetwork()
        {
            // Poll net messages here
            if (app.hostingServer != null)
            {
                app.hostingServer.onNetwork();
            }

            ReferenceCountedPointer<ReliableConduit> net = serverProxy.getNet();
            if (net == null || net.pointer() == null)
            {
                return;
            }

            while (net.pointer().messageWaiting() && net.pointer().ok())
            {
                switch ((MessageType)net.pointer().waitingMessageType())
                {
                    case MessageType.NO_MSG:
                        // No message waiting
                        break;

                    case MessageType.SignOnMessage_MSG:
                        {
                            SignOnMessage msg = new SignOnMessage();
                            net.pointer().receive(msg);
                            localID = msg.id;
                        }
                        break;

                    case MessageType.CreateEntityMessage_MSG:
                        {
                            Entity entity = new Entity();
                            net.pointer().receive(entity);
                            entityTable.set(entity.id, entity);

                            if (localID == entity.id)
                            {
                                // This is the client's own entity.  The original set the
                                // window caption; the port's window contract has none.
                            }
                            app.debugLog.printf("CLIENT: Created entity \"%s\"\n\n",
                                entity.name);
                        }
                        break;

                    case MessageType.EntityStateMessage_MSG:
                        {
                            EntityStateMessage msg = new EntityStateMessage();
                            net.pointer().receive(msg);
                            Entity entity;
                            if (entityTable.get(msg.id, out entity))
                            {
                                entity.clientUpdateFromStateMessage(msg, localID);
                            }
                        }
                        break;

                    default:
                        app.debugLog.printf("CLIENT: Ignored unknown message type %d\n",
                            net.pointer().waitingMessageType());
                        net.pointer().receive();
                        break;
                }
            }
        }

        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            if (app.hostingServer != null)
            {
                app.hostingServer.onSimulation(rdt, sdt, idt);
            }

            EntitySimulation.simulateEntities(entityTable, sdt);

            simulateCamera(sdt);
        }

        public override void onUserInput(UserInput ui)
        {
            if (ui.keyPressed(UserInput.KeyCode.ESCAPE))
            {
                // Quit back to main menu
                endApplet = true;
            }

            Entity entity;
            if (entityTable.get(localID, out entity) && !app.debugController.active() && app.userInput.appHasFocus())
            {
                Controls newControls = entity.controls;

                const int UP_KEY1 = ' ';
                const UserInput.KeyCode UP_KEY2 = UserInput.KeyCode.BACKSPACE;
                const int DOWN_KEY1 = 'z';
                const UserInput.KeyCode DOWN_KEY2 = UserInput.KeyCode.LCTRL;

                // Construct the local reference frame vectors in the world frame
                // (ignores roll and pitch of the aircraft).
                Vector3 localY = Vector3.unitY();
                Vector3 localX = entity.coordinateFrame().rightVector();
                localX = (localX - localY * localX.dot(localY)).direction();
                Vector3 localZ = localX.cross(localY);

                const double speed = 2;

                double dx = app.userInput.getX();
                double dz = app.userInput.getY();
                double mag = Math.Sqrt(g3dmath.square(dx) + g3dmath.square(dz));
                if (mag > 0)
                {
                    newControls.desiredVelocity =
                        (localX * (float)app.userInput.getX() +
                         -localZ * (float)app.userInput.getY()) * (float)(speed / mag);
                }
                else
                {
                    newControls.desiredVelocity = Vector3.zero();
                }

                // The vertical axis is independent of the others
                if (app.userInput.keyDown((UserInput.KeyCode)(int)UP_KEY1) ||
                    app.userInput.keyDown((UserInput.KeyCode)(int)UP_KEY2))
                {
                    newControls.desiredVelocity.y = (float)(speed / 2);
                }
                else if (app.userInput.keyDown((UserInput.KeyCode)(int)DOWN_KEY1) ||
                         app.userInput.keyDown((UserInput.KeyCode)(int)DOWN_KEY2))
                {
                    newControls.desiredVelocity.y = (float)(-speed / 2);
                }
                else
                {
                    newControls.desiredVelocity.y = 0;
                }

                // Normalized [-1,-1] to [1,1] mouse
                Vector2 mouse = -((app.userInput.mouseXY() /
                    new Vector2((float)app.window().width(), (float)app.window().height())) * 2 -
                    new Vector2(1, 1));

                double mx = mouse.x;
                // Create a dead zone
                mx = Math.Max(0.0, Math.Abs(mx) - 0.25) * g3dmath.sign(mx);

                newControls.desiredYawVelocity = mx * g3dmath.toRadians(180);

                if (entity.controls != newControls)
                {
                    // Tell the server our new controls
                    app.debugPrintf("CLIENT: send");
                    entity.oldDesiredVelocity = entity.currentTiltVelocity;
                    entity.oldDesiredVelocityTime = global::G3D.System.time();
                    entity.controls = newControls;
                    EntityStateMessage msg = new EntityStateMessage();
                    entity.makeStateMessage(msg);
                    serverProxy.getNet().pointer().send((uint)MessageType.EntityStateMessage_MSG, msg);
                }
            }

            // Add other key handling here
        }

        public override void onGraphics(RenderDevice rd)
        {
            GCamera cam = app.debugController.active() ? app.debugCamera : camera;

            LightingParameters lighting = new LightingParameters(G3DGameUnits.toSeconds(11, 00, 00, AMPM.AM));
            app.renderDevice.setProjectionAndCameraMatrix(cam);

            // Cyan background
            app.renderDevice.setColorClearValue(new Color4(new Color3(0.1f, 0.5f, 1.0f), 1));

            app.renderDevice.clear(app.sky == null, true, true);
            if (app.sky != null)
            {
                app.sky.pointer().render(lighting);
            }

            // Setup lighting
            app.renderDevice.enableLighting();
            app.renderDevice.setLight(0, GLight.directional(lighting.lightDirection, lighting.lightColor));
            // Splash light
            app.renderDevice.setLight(1, GLight.directional(-Vector3.unitY(), Color3.white() * 0.20f, false));
            app.renderDevice.setAmbientLightColor(lighting.ambient);

            Draw.axes(new CoordinateFrame(new Vector3(0, 4, 0)), app.renderDevice);

            for (int x = -10; x < 10; x += 4)
            {
                for (int z = -10; z < 10; z += 4)
                {
                    Vector3 v = new Vector3(x * 20, -10, z * 20);
                    Draw.box(new Box(v, v + new Vector3(2, .2f, 2)), app.renderDevice, new Color4(Color3.red(), 1), new Color4(Color3.black(), 1));
                }
            }

            renderEntities();

            app.renderDevice.disableLighting();

            if (app.sky != null)
            {
                app.sky.pointer().renderLensFlare(lighting);
            }

            app.renderDevice.push2D();

            // Entity names
            {
                Table<int, Entity>.Iterator end = entityTable.end();
                for (Table<int, Entity>.Iterator e = entityTable.begin(); e != end; e.MoveNext())
                {
                    Entity entity = e.Current.value;
                    Vector3 pos = projectToScreen(entity.smoothCoordinateFrame().translation);

                    if (pos.isFinite())
                    {
                        app.font.pointer().draw2D(app.renderDevice, entity.name, pos.xy(), 16,
                            new Color4(Color3.white(), 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_CENTER);
                    }
                }
            }

            // Server crash messages
            if (!serverProxy.ok())
            {
                app.font.pointer().draw2D(app.renderDevice, "Lost connection to server.", new Vector2(5, 5), 14,
                    new Color4(new Color3(1.0f, 0.4f, 0.4f), 1), new Color4(Color3.black(), 1));
            }

            if (app.hostingServer != null && !app.hostingServer.ok())
            {
                app.font.pointer().draw2D(app.renderDevice, "(Local Server Crashed)", new Vector2(5, 25), 14,
                    new Color4(new Color3(1.0f, 0.4f, 0.4f), 1), new Color4(Color3.black(), 1));
            }
            app.renderDevice.pop2D();

            if (app.hostingServer != null)
            {
                app.hostingServer.onGraphics(rd);
            }
        }
    }
}
