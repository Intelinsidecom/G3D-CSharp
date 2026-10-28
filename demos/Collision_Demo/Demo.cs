/**
  @file Collision_Demo/Demo.cs

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @created 2003-02-07
  @edited  2003-11-18
*/

using System;
using G3D;

namespace Collision_Demo
{
    /**
     This simple demo applet uses the debug mode as the regular
     rendering mode so you can fly around the scene.
    */
    public class Demo : GApplet
    {
    private void insertRestingContactSpheres()
    {
        scene.insertDynamic(new SimSphere(new Sphere(new Vector3(4, 1.01f, 3), 1), Vector3.zero(), Color3.blue()));
        scene.insertDynamic(new SimSphere(new Sphere(new Vector3(4, .49f, 4.5f), .5f), Vector3.zero(), Color3.blue()));
        scene.insertDynamic(new SimSphere(new Sphere(new Vector3(4, 2, 1), .5f), Vector3.zero(), Color3.blue()));
    }

    private void insertTray()
    {
        float wallHeight = 4;
        Color3 trayColor = Color3.gray();
        scene.insertStatic(new BoxObject(new Box(new Vector3(-14, -0.5f, -10), new Vector3(14, 0, 10)), trayColor));
        scene.insertStatic(new BoxObject(new Box(new Vector3(-14, 0, -10), new Vector3(-13, wallHeight, 10)), trayColor));
        scene.insertStatic(new BoxObject(new Box(new Vector3(13, 0, -10), new Vector3(14, wallHeight, 10)), trayColor));
        scene.insertStatic(new BoxObject(new Box(new Vector3(-13, 0, -10), new Vector3(13, wallHeight, -9)), trayColor));
        scene.insertStatic(new BoxObject(new Box(new Vector3(-13, 0, 9), new Vector3(13, wallHeight, 10)), trayColor));
    }

    private void insertRollingContactSpheres()
    {
        scene.insertDynamic(new SimSphere(new Sphere(new Vector3(-10, .25f, -2), .25f), new Vector3(16, 0, 4), Color3.blue()));
        scene.insertDynamic(new SimSphere(new Sphere(new Vector3(-2.25f, 4.75f, 9.4f), .25f), Vector3.zero(), Color3.blue()));
    }

    private void insertSpiralSlide()
    {
        for (int i = 0; i < 41; ++i)
        {
            double angle = g3dmath.pi() * i / 10.0;
            double angle2 = g3dmath.pi() * (i - 0.6) / 10.0;

            CoordinateFrame c = CoordinateFrame.identity();
            Box b = new Box(new Vector3(-1, -1, -.1f), new Vector3(1, 1, .1f));
            c.translation = new Vector3((float)(Math.Cos(angle) * 2.9f), (float)(i / 3.5f + 1.5f), (float)(Math.Sin(angle) * 2.9f));
            c.lookAt(new Vector3((float)(Math.Cos(angle2) * 1.5f), (float)(i / 3.5 + 2.2f), (float)(Math.Sin(angle2) * 1.5f)));
            scene.insertStatic(new BoxObject(c.toWorldSpace(b), (Color3.yellow() + Color3.white()) / 2.0f));

            {
                Box b2 = new Box(new Vector3(-.3f, -.3f, -.1f), new Vector3(.25f, .25f, .1f));
                CoordinateFrame c2 = CoordinateFrame.identity();
                c2.translation = new Vector3((float)(Math.Cos(angle) * 1.2f), (float)(i / 3.5f + 1), (float)(Math.Sin(angle) * 1.2f));
                c2.lookAt(new Vector3((float)(Math.Cos(angle2) * 3), (float)(i / 3.5f + 2), (float)(Math.Sin(angle2) * 3)));
                scene.insertStatic(new BoxObject(c2.toWorldSpace(b2), (Color3.yellow() + Color3.white()) / 2.0f));
            }
        }

        scene.insertDynamic(new SimSphere(new Sphere(new Vector3(1.9f, 13, -1), .75f), new Vector3(-2, -.5f, -2), Color3.blue()));
    }

    private void insertRamps()
    {
        {
            Box b = new Box(new Vector3(-1, 0, -5), new Vector3(1, .25f, 5.5f));
            CoordinateFrame c = CoordinateFrame.identity();
            c.lookAt(new Vector3(0, 1, 2));
            c.translation = new Vector3(-2.5f, 2.25f, 5.5f);
            scene.insertStatic(new BoxObject(c.toWorldSpace(b), (Color3.green() + Color3.white()) / 2.0f));
        }

        {
            Box b = new Box(new Vector3(-1, 0, -5), new Vector3(1, .25f, 5.5f));
            CoordinateFrame c = CoordinateFrame.identity();
            c.lookAt(new Vector3(-2, 2, -2));
            c.translation = new Vector3(-11.2f, 2.85f, -7.2f);
            scene.insertStatic(new BoxObject(c.toWorldSpace(b), (Color3.green() + Color3.white()) / 2.0f));
        }
    }

        private void buildScene()
        {
            scene.insertStatic(new BoxObject(new Box(new Vector3(6, 0, 0), new Vector3(7, 1, 8)), (Color3.green() + Color3.white()) / 2.0f));
            scene.insertStatic(new GeneralObject(Model.getModel("cow.ifs"), new CoordinateFrame(new Vector3(-7, 1.7f, 4)), Color3.yellow()));
            scene.insertStatic(new GeneralObject(Model.getModel("p51-mustang.ifs"), new CoordinateFrame(new Vector3(10, 1, 2)), Color3.orange()));
            scene.insertStatic(new GeneralObject(Model.getModel("knot.ifs"), new CoordinateFrame(new Vector3(7, 2.2f, -4)), (Color3.blue() + Color3.white()) / 2.0f));

            insertTray();
            insertRamps();
            insertSpiralSlide();

            int i;
            scene.insertDynamic(new SimSphere(new Sphere(new Vector3(0, 7, 0), .5f),
                Vector3.random() * 10,
                Color3.wheelRandom()));
            for (i = 0; i < 5; ++i)
            {
                scene.insertDynamic(new SimSphere(new Sphere(new Vector3(0, 7, 0), .25f),
                    Vector3.random() * 10,
                    Color3.wheelRandom()));
            }
        }

        // Add state that should be visible to this applet.
        // If you have multiple applets that need to share
        // state, put it in the App.

        public App app;

        public Scene scene;

        /** World time */
        public GameTime gameTime;

        /** Reused lighting parameters, updated once per frame. */
        private LightingParameters lighting = new LightingParameters();

        /** Time at which the simulation started; used to reset when it gets boring.*/
        public RealTime simStartTime;

        public Demo(App _app) : base(_app)
        {
            app = _app;
            scene = new Scene(app);
            Model.app = app;
            gameTime = 0;
        }

        public override void onInit()
        {
            app.debugCamera.setPosition(new Vector3(15, 20, 15));
            app.debugCamera.lookAt(new Vector3(-2, 3, -5));
            app.debugController.setActive(false);

            buildScene();

            gameTime = G3DGameUnits.toSeconds(10, 0, 0, AMPM.AM);

            setDesiredFrameRate(60);
            simStartTime = G3D.System.time();
        }

        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            if (simStartTime + 10 < G3D.System.time())
            {
                // Periodically reset
                scene.clear();
                buildScene();
                simStartTime = G3D.System.time();
            }

            scene.simulate(new GameTime(idt));
        }

        public override void onWait(RealTime t, RealTime desiredT)
        {
            double remaining = desiredT - t;
            if (remaining <= 0.0)
            {
                return;
            }

            double start = G3D.System.time();
            if (remaining > 0.003)
            {
                G3D.System.sleep(remaining - 0.002);
            }
            while (G3D.System.time() - start < remaining)
            {
            }
        }

        public override void onGraphics(RenderDevice rd)
        {
            rd.pushState();

            rd.setProjectionAndCameraMatrix(app.debugCamera);

            lighting.setTime(gameTime);
            lighting.lightDirection = (lighting.lightDirection + new Vector3(0, 0, .4f)).direction();
            lighting.ambient = lighting.ambient + new Color3(.3f, .3f, .4f);

            scene.render(lighting);

            rd.popState();
        }
    }
}